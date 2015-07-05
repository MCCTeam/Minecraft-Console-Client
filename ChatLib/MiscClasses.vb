
Imports MinecraftClient
Imports MinecraftClient.Protocol

Public Class RawMinecraftMessage

End Class

Public Class UserAccount

    Public SessionId As String = ""
    Public UUID As String = ""
    Public OfflineMode As Boolean = False
    Public LoginUser As String
    Public LoginPassword As String
    Public LoginOk As Boolean
    Public Name As String


    Sub New(login As String, Optional password As String = "-", Optional autoLogin As Boolean = False)

        LoginUser = login
        LoginPassword = password
        OfflineMode = (password = "-")

        If autoLogin Then
            Dim lr = Me.Login
            If lr <> ProtocolHandler.LoginResult.Success Then
                Throw New Exception("Login failed with state: " & lr.ToString)
                'Else
            End If
        End If

    End Sub

    Function Login() As ProtocolHandler.LoginResult

        sessionID = ""
        UUID = ""

        Dim ret As ProtocolHandler.LoginResult = ProtocolHandler.LoginResult.OtherError

        If OfflineMode Then
            ret = ProtocolHandler.LoginResult.Success
            sessionID = "0"
            Name = LoginUser
            ConsoleIO.WriteLineFormatted("§8You chose to run in offline mode.")
        Else
            Console.WriteLine("Connecting to Minecraft.net...")
            ret = ProtocolHandler.GetLogin(LoginUser, LoginPassword, sessionID, UUID)
        End If

        If ret = ProtocolHandler.LoginResult.Success Then
            loginOk = True
            Console.WriteLine((Convert.ToString("Success. (session ID: ") & sessionID) + ")"c)
        End If

        Return ret
    End Function


End Class