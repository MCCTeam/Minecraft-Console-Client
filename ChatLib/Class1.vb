Imports System.Runtime.CompilerServices
Imports MinecraftClient
Imports MinecraftClient.Protocol

Public Class MinecraftConnection

    Dim Client As MinecraftClient.McTcpClient
    Dim user As UserAccount

    Event NewMessage(ByVal text As RawMinecraftMessage)

    Sub New(username As String, Optional password As String = "-")
        user = New UserAccount(username, password)

    

    End Sub



    'Private Shared Sub jugfjdgfhjghf()




    '    If Settings.ServerIP = "" Then
    '        Console.Write("Server IP : ")
    '        Settings.SetServerIP(Console.ReadLine())
    '    End If

    '    'Get server version
    '    Dim protocolversion As Integer = 0

    '    If Settings.ServerVersion <> "" AndAlso Settings.ServerVersion.ToLower() <> "auto" Then
    '        protocolversion = Protocol.ProtocolHandler.MCVer2ProtocolVersion(Settings.ServerVersion)

    '        If protocolversion <> 0 Then
    '            ConsoleIO.WriteLineFormatted("§8Using Minecraft version " + Settings.ServerVersion + " (protocol v" + protocolversion + ")"c)
    '        Else
    '            ConsoleIO.WriteLineFormatted("§8Unknown or not supported MC version '" + Settings.ServerVersion + "'." & vbLf & "Switching to autodetection mode.")
    '        End If

    '        If useMcVersionOnce Then
    '            useMcVersionOnce = False
    '            Settings.ServerVersion = ""
    '        End If
    '    End If

    '    If protocolversion = 0 Then
    '        Console.WriteLine("Retrieving Server Info...")
    '        If Not ProtocolHandler.GetServerInfo(Settings.ServerIP, Settings.ServerPort, protocolversion) Then
    '            HandleFailure("Failed to ping this IP.", True, ChatBots.AutoRelog.DisconnectReason.ConnectionLost)
    '            Return
    '        End If
    '    End If

    '    If protocolversion <> 0 Then
    '        Try
    '            'Start the main TCP client
    '            If Settings.SingleCommand <> "" Then
    '                Client = New McTcpClient(Settings.Username, UUID, sessionID, Settings.ServerIP, Settings.ServerPort, protocolversion, _
    '                    Settings.SingleCommand)
    '            Else
    '                Client = New McTcpClient(Settings.Username, UUID, sessionID, protocolversion, Settings.ServerIP, Settings.ServerPort)
    '            End If

    '            'Update console title
    '            If Settings.ConsoleTitle <> "" Then
    '                Console.Title = Settings.ExpandVars(Settings.ConsoleTitle)
    '            End If
    '        Catch generatedExceptionName As NotSupportedException
    '            HandleFailure("Cannot connect to the server : This version is not supported !", True)
    '        End Try
    '    Else
    '        HandleFailure("Failed to determine server version.", True)
    '    End If
    '    Else
    '    Console.ForegroundColor = ConsoleColor.Gray
    '    Dim failureMessage As String = "Minecraft Login failed : "
    '    Select Case result
    '        Case ProtocolHandler.LoginResult.AccountMigrated
    '            failureMessage += "Account migrated, use e-mail as username."
    '            Exit Select
    '        Case ProtocolHandler.LoginResult.ServiceUnavailable
    '            failureMessage += "Login servers are unavailable. Please try again later."
    '            Exit Select
    '        Case ProtocolHandler.LoginResult.WrongPassword
    '            failureMessage += "Incorrect password."
    '            Exit Select
    '        Case ProtocolHandler.LoginResult.NotPremium
    '            failureMessage += "User not premium."
    '            Exit Select
    '        Case ProtocolHandler.LoginResult.OtherError
    '            failureMessage += "Network error."
    '            Exit Select
    '        Case ProtocolHandler.LoginResult.SSLError
    '            failureMessage += "SSL Error."
    '            Exit Select
    '        Case Else
    '            failureMessage += "Unknown Error."
    '            Exit Select
    '    End Select
    '    If result = ProtocolHandler.LoginResult.SSLError AndAlso isUsingMono Then
    '        ConsoleIO.WriteLineFormatted("§8It appears that you are using Mono to run this program." + ControlChars.Lf + "The first time, you have to import HTTPS certificates using:" + ControlChars.Lf + "mozroots --import --ask-remove")
    '        Return
    '    End If
    '    HandleFailure(failureMessage, False, ChatBot.DisconnectReason.LoginRejected)
    '    End If
    'End Sub



End Class
