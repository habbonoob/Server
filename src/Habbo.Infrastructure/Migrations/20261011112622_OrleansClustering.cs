using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Habbo.Infrastructure.Migrations
{
    /// <summary>The tables Orleans keeps its cluster's membership in, for silos and gateways in processes of their own
    /// (Orleans:Clustering postgres): Orleans' own scripts for PostgreSQL, as released with Orleans 10.3.1
    /// (src/AdoNet/Shared/PostgreSQL-Main.sql and src/AdoNet/Orleans.Clustering.AdoNet/PostgreSQL-Clustering.sql).</summary>
    public partial class OrleansClustering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Main);
            migrationBuilder.Sql(Clustering);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP FUNCTION IF EXISTS update_i_am_alive_time;
DROP FUNCTION IF EXISTS insert_membership_version;
DROP FUNCTION IF EXISTS insert_membership;
DROP FUNCTION IF EXISTS update_membership;
DROP TABLE IF EXISTS OrleansMembershipTable;
DROP TABLE IF EXISTS OrleansMembershipVersionTable;
DROP TABLE IF EXISTS OrleansQuery;");
        }

        private const string Main = @"
-- requires Postgres 9.5 (or perhaps higher)

/*
Implementation notes:

1) The general idea is that data is read and written through Orleans specific queries.
   Orleans operates on column names and types when reading and on parameter names and types when writing.

2) The implementations *must* preserve input and output names and types. Orleans uses these parameters to reads query results by name and type.
   Vendor and deployment specific tuning is allowed and contributions are encouraged as long as the interface contract
   is maintained.

3) The implementation across vendor specific scripts *should* preserve the constraint names. This simplifies troubleshooting
   by virtue of uniform naming across concrete implementations.

5) ETag for Orleans is an opaque column that represents a unique version. The type of its actual implementation
   is not important as long as it represents a unique version. In this implementation we use integers for versioning

6) For the sake of being explicit and removing ambiguity, Orleans expects some queries to return either TRUE as >0 value
   or FALSE as =0 value. That is, affected rows or such does not matter. If an error is raised or an exception is thrown
   the query *must* ensure the entire transaction is rolled back and may either return FALSE or propagate the exception.
   Orleans handles exception as a failure and will retry.

7) The implementation follows the Extended Orleans membership protocol. For more information, see at:
        https://learn.microsoft.com/dotnet/orleans/implementation/cluster-management
        https://github.com/dotnet/orleans/blob/main/src/Orleans.Core/SystemTargetInterfaces/IMembershipTable.cs
*/



-- This table defines Orleans operational queries. Orleans uses these to manage its operations,
-- these are the only queries Orleans issues to the database.
-- These can be redefined (e.g. to provide non-destructive updates) provided the stated interface principles hold.
CREATE TABLE OrleansQuery
(
    QueryKey varchar(64) NOT NULL,
    QueryText varchar(8000) NOT NULL,

    CONSTRAINT OrleansQuery_Key PRIMARY KEY(QueryKey)
);
";

        private const string Clustering = @"
-- For each deployment, there will be only one (active) membership version table version column which will be updated periodically.
CREATE TABLE OrleansMembershipVersionTable
(
    DeploymentId varchar(150) NOT NULL,
    Timestamp timestamptz(3) NOT NULL DEFAULT now(),
    Version integer NOT NULL DEFAULT 0,

    CONSTRAINT PK_OrleansMembershipVersionTable_DeploymentId PRIMARY KEY(DeploymentId)
);

-- Every silo instance has a row in the membership table.
CREATE TABLE OrleansMembershipTable
(
    DeploymentId varchar(150) NOT NULL,
    Address varchar(45) NOT NULL,
    Port integer NOT NULL,
    Generation integer NOT NULL,
    SiloName varchar(150) NOT NULL,
    HostName varchar(150) NOT NULL,
    Status integer NOT NULL,
    ProxyPort integer NULL,
    SuspectTimes varchar(8000) NULL,
    StartTime timestamptz(3) NOT NULL,
    IAmAliveTime timestamptz(3) NOT NULL,

    CONSTRAINT PK_MembershipTable_DeploymentId PRIMARY KEY(DeploymentId, Address, Port, Generation),
    CONSTRAINT FK_MembershipTable_MembershipVersionTable_DeploymentId FOREIGN KEY (DeploymentId) REFERENCES OrleansMembershipVersionTable (DeploymentId)
);

CREATE FUNCTION update_i_am_alive_time(
    deployment_id OrleansMembershipTable.DeploymentId%TYPE,
    address_arg OrleansMembershipTable.Address%TYPE,
    port_arg OrleansMembershipTable.Port%TYPE,
    generation_arg OrleansMembershipTable.Generation%TYPE,
    i_am_alive_time OrleansMembershipTable.IAmAliveTime%TYPE)
  RETURNS void AS
$func$
BEGIN
    -- This is expected to never fail by Orleans, so return value
    -- is not needed nor is it checked.
    UPDATE OrleansMembershipTable as d
    SET
        IAmAliveTime = i_am_alive_time
    WHERE
        d.DeploymentId = deployment_id AND deployment_id IS NOT NULL
        AND d.Address = address_arg AND address_arg IS NOT NULL
        AND d.Port = port_arg AND port_arg IS NOT NULL
        AND d.Generation = generation_arg AND generation_arg IS NOT NULL;
END
$func$ LANGUAGE plpgsql;

INSERT INTO OrleansQuery(QueryKey, QueryText)
VALUES
(
    'UpdateIAmAlivetimeKey','
    -- This is expected to never fail by Orleans, so return value
    -- is not needed nor is it checked.
    SELECT * from update_i_am_alive_time(
        @DeploymentId,
        @Address,
        @Port,
        @Generation,
        @IAmAliveTime
    );
');

CREATE FUNCTION insert_membership_version(
    DeploymentIdArg OrleansMembershipTable.DeploymentId%TYPE
)
  RETURNS TABLE(row_count integer) AS
$func$
DECLARE
    RowCountVar int := 0;
BEGIN

    BEGIN

        INSERT INTO OrleansMembershipVersionTable
        (
            DeploymentId
        )
        SELECT DeploymentIdArg
        ON CONFLICT (DeploymentId) DO NOTHING;

        GET DIAGNOSTICS RowCountVar = ROW_COUNT;

        ASSERT RowCountVar <> 0, 'no rows affected, rollback';

        RETURN QUERY SELECT RowCountVar;
    EXCEPTION
    WHEN assert_failure THEN
        RETURN QUERY SELECT RowCountVar;
    END;

END
$func$ LANGUAGE plpgsql;

INSERT INTO OrleansQuery(QueryKey, QueryText)
VALUES
(
    'InsertMembershipVersionKey','
    SELECT * FROM insert_membership_version(
        @DeploymentId
    );
');

CREATE FUNCTION insert_membership(
    DeploymentIdArg OrleansMembershipTable.DeploymentId%TYPE,
    AddressArg      OrleansMembershipTable.Address%TYPE,
    PortArg         OrleansMembershipTable.Port%TYPE,
    GenerationArg   OrleansMembershipTable.Generation%TYPE,
    SiloNameArg     OrleansMembershipTable.SiloName%TYPE,
    HostNameArg     OrleansMembershipTable.HostName%TYPE,
    StatusArg       OrleansMembershipTable.Status%TYPE,
    ProxyPortArg    OrleansMembershipTable.ProxyPort%TYPE,
    StartTimeArg    OrleansMembershipTable.StartTime%TYPE,
    IAmAliveTimeArg OrleansMembershipTable.IAmAliveTime%TYPE,
    VersionArg      OrleansMembershipVersionTable.Version%TYPE)
  RETURNS TABLE(row_count integer) AS
$func$
DECLARE
    RowCountVar int := 0;
BEGIN

    BEGIN
        INSERT INTO OrleansMembershipTable
        (
            DeploymentId,
            Address,
            Port,
            Generation,
            SiloName,
            HostName,
            Status,
            ProxyPort,
            StartTime,
            IAmAliveTime
        )
        SELECT
            DeploymentIdArg,
            AddressArg,
            PortArg,
            GenerationArg,
            SiloNameArg,
            HostNameArg,
            StatusArg,
            ProxyPortArg,
            StartTimeArg,
            IAmAliveTimeArg
        ON CONFLICT (DeploymentId, Address, Port, Generation) DO
            NOTHING;


        GET DIAGNOSTICS RowCountVar = ROW_COUNT;

        UPDATE OrleansMembershipVersionTable
        SET
            Timestamp = now(),
            Version = Version + 1
        WHERE
            DeploymentId = DeploymentIdArg AND DeploymentIdArg IS NOT NULL
            AND Version = VersionArg AND VersionArg IS NOT NULL
            AND RowCountVar > 0;

        GET DIAGNOSTICS RowCountVar = ROW_COUNT;

        ASSERT RowCountVar <> 0, 'no rows affected, rollback';


        RETURN QUERY SELECT RowCountVar;
    EXCEPTION
    WHEN assert_failure THEN
        RETURN QUERY SELECT RowCountVar;
    END;

END
$func$ LANGUAGE plpgsql;

INSERT INTO OrleansQuery(QueryKey, QueryText)
VALUES
(
    'InsertMembershipKey','
    SELECT * FROM insert_membership(
        @DeploymentId,
        @Address,
        @Port,
        @Generation,
        @SiloName,
        @HostName,
        @Status,
        @ProxyPort,
        @StartTime,
        @IAmAliveTime,
        @Version
    );
');

CREATE FUNCTION update_membership(
    DeploymentIdArg OrleansMembershipTable.DeploymentId%TYPE,
    AddressArg      OrleansMembershipTable.Address%TYPE,
    PortArg         OrleansMembershipTable.Port%TYPE,
    GenerationArg   OrleansMembershipTable.Generation%TYPE,
    StatusArg       OrleansMembershipTable.Status%TYPE,
    SuspectTimesArg OrleansMembershipTable.SuspectTimes%TYPE,
    IAmAliveTimeArg OrleansMembershipTable.IAmAliveTime%TYPE,
    VersionArg      OrleansMembershipVersionTable.Version%TYPE
  )
  RETURNS TABLE(row_count integer) AS
$func$
DECLARE
    RowCountVar int := 0;
BEGIN

    BEGIN

    UPDATE OrleansMembershipVersionTable
    SET
        Timestamp = now(),
        Version = Version + 1
    WHERE
        DeploymentId = DeploymentIdArg AND DeploymentIdArg IS NOT NULL
        AND Version = VersionArg AND VersionArg IS NOT NULL;


    GET DIAGNOSTICS RowCountVar = ROW_COUNT;

    UPDATE OrleansMembershipTable
    SET
        Status = StatusArg,
        SuspectTimes = SuspectTimesArg,
        IAmAliveTime = IAmAliveTimeArg
    WHERE
        DeploymentId = DeploymentIdArg AND DeploymentIdArg IS NOT NULL
        AND Address = AddressArg AND AddressArg IS NOT NULL
        AND Port = PortArg AND PortArg IS NOT NULL
        AND Generation = GenerationArg AND GenerationArg IS NOT NULL
        AND RowCountVar > 0;


        GET DIAGNOSTICS RowCountVar = ROW_COUNT;

        ASSERT RowCountVar <> 0, 'no rows affected, rollback';


        RETURN QUERY SELECT RowCountVar;
    EXCEPTION
    WHEN assert_failure THEN
        RETURN QUERY SELECT RowCountVar;
    END;

END
$func$ LANGUAGE plpgsql;

INSERT INTO OrleansQuery(QueryKey, QueryText)
VALUES
(
    'UpdateMembershipKey','
    SELECT * FROM update_membership(
        @DeploymentId,
        @Address,
        @Port,
        @Generation,
        @Status,
        @SuspectTimes,
        @IAmAliveTime,
        @Version
    );
');

INSERT INTO OrleansQuery(QueryKey, QueryText)
VALUES
(
    'MembershipReadRowKey','
    SELECT
        v.DeploymentId,
        m.Address,
        m.Port,
        m.Generation,
        m.SiloName,
        m.HostName,
        m.Status,
        m.ProxyPort,
        m.SuspectTimes,
        m.StartTime,
        m.IAmAliveTime,
        v.Version
    FROM
        OrleansMembershipVersionTable v
        -- This ensures the version table will returned even if there is no matching membership row.
        LEFT OUTER JOIN OrleansMembershipTable m ON v.DeploymentId = m.DeploymentId
        AND Address = @Address AND @Address IS NOT NULL
        AND Port = @Port AND @Port IS NOT NULL
        AND Generation = @Generation AND @Generation IS NOT NULL
    WHERE
        v.DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
');

INSERT INTO OrleansQuery(QueryKey, QueryText)
VALUES
(
    'MembershipReadAllKey','
    SELECT
        v.DeploymentId,
        m.Address,
        m.Port,
        m.Generation,
        m.SiloName,
        m.HostName,
        m.Status,
        m.ProxyPort,
        m.SuspectTimes,
        m.StartTime,
        m.IAmAliveTime,
        v.Version
    FROM
        OrleansMembershipVersionTable v LEFT OUTER JOIN OrleansMembershipTable m
        ON v.DeploymentId = m.DeploymentId
    WHERE
        v.DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
');

INSERT INTO OrleansQuery(QueryKey, QueryText)
VALUES
(
    'DeleteMembershipTableEntriesKey','
    DELETE FROM OrleansMembershipTable
    WHERE DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
    DELETE FROM OrleansMembershipVersionTable
    WHERE DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
');

INSERT INTO OrleansQuery(QueryKey, QueryText)
VALUES
(
    'GatewaysQueryKey','
    SELECT
        Address,
        ProxyPort,
        Generation
    FROM
        OrleansMembershipTable
    WHERE
        DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
        AND Status = @Status AND @Status IS NOT NULL
        AND ProxyPort > 0;
');

INSERT INTO OrleansQuery(QueryKey, QueryText)
VALUES
(
    'CleanupDefunctSiloEntriesKey',
    'DELETE FROM OrleansMembershipTable
    WHERE DeploymentId = @DeploymentId
        AND @DeploymentId IS NOT NULL
        AND IAmAliveTime < @IAmAliveTime
        AND Status != 3;
');
";
    }
}
