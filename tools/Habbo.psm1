<#
.SYNOPSIS
    Dev helpers for the Habbo emulator.

.DESCRIPTION
    Import from your PowerShell profile:

        Import-Module <path to this repository>\tools\Habbo.psm1

    Then `ticket` prints a fresh SSO ticket for the admin account.
#>

function Get-HabboTicket {
    <#
    .SYNOPSIS
        Gets a fresh SSO ticket from the running server.

    .EXAMPLE
        ticket
        Prints a ticket for admin.

    .EXAMPLE
        ticket tester
        Prints a ticket for the tester account.

    .EXAMPLE
        ticket -Copy
        Copies the ticket to the clipboard instead of printing it.

    .EXAMPLE
        ticket -Full
        Prints the ticket together with its expiry time.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Position = 0)]
        [string] $Username = 'admin',

        [Parameter(Position = 1)]
        [string] $Password = 'password',

        # The host's web listener; override when it is not on the default port.
        [string] $BaseUrl = 'http://localhost:8080',

        # Copy the ticket to the clipboard rather than writing it out.
        [switch] $Copy,

        # Return the whole response (ticket plus expiry) instead of just the ticket.
        [switch] $Full
    )

    $body = @{ username = $Username; password = $Password } | ConvertTo-Json -Compress

    try {
        $response = Invoke-RestMethod -Method Post -Uri "$BaseUrl/api/auth/login" `
            -ContentType 'application/json' -Body $body -ErrorAction Stop
    }
    catch {
        # The two failure modes worth telling apart: server down vs. credentials rejected.
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq 401) {
            Write-Error "Login refused for '$Username'. Check the username and password."
        }
        elseif ($null -eq $status) {
            Write-Error "No answer from $BaseUrl. Is Habbo.Host running?"
        }
        else {
            Write-Error "Login failed with HTTP $status."
        }
        return
    }

    if ($Full) {
        return [pscustomobject]@{
            Username  = $Username
            Ticket    = $response.ssoTicket
            ExpiresAt = $response.expiresAt
        }
    }

    if ($Copy) {
        Set-Clipboard -Value $response.ssoTicket
        Write-Host "Ticket for '$Username' copied to clipboard." -ForegroundColor Green
        return
    }

    $response.ssoTicket
}

Set-Alias -Name ticket -Value Get-HabboTicket

Export-ModuleMember -Function Get-HabboTicket -Alias ticket
