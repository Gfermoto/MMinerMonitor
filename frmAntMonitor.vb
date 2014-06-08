﻿Imports MAntMonitor.Extensions

Public Class frmAntMonitor

    Private wb(0 To 2) As WebBrowser

    Private RebootInfo As System.Collections.Generic.Dictionary(Of String, Date)

    Private ds As DataSet
    
    Private Const csRegKey As String = "Software\MAntMonitor"

    Private Const csVersion As String = "M's Ant Monitor v1.6"

    Private iCountDown, iWatchDog, bAnt As Integer

    Private iRefreshRate As Integer

    Private ctlsByKey As ControlsByRegistry

    Private bStarted As Boolean

    Private Enum enAntType
        S1
        S2
    End Enum

#If DEBUG Then
    Private Const bErrorHandle As Boolean = False
#Else
    Private Const bErrorHandle As Boolean = True
#End If

    Private Sub Form1_Load(sender As Object, e As System.EventArgs) Handles Me.Load

        Dim host As System.Net.IPHostEntry

        AddToLog(csVersion & " starting")

        bStarted = True

        host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName)

        For Each IP As System.Net.IPAddress In host.AddressList
            If IP.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then
                Me.cmbLocalIPs.Items.Add(IP.ToString)
            End If
        Next

        Me.cmbLocalIPs.Text = Me.cmbLocalIPs.Items(0)

        RebootInfo = New System.Collections.Generic.Dictionary(Of String, Date)

        ds = New DataSet

        With ds
            .Tables.Add()
            Me.dataAnts.DataSource = .Tables(0)

            With .Tables(0).Columns
                .Add("Name")
                .Add("Uptime")
                .Add("GH/s(5s)")
                .Add("GH/s(avg)")
                .Add("Blocks")
                .Add("HWE%")
                .Add("BestShare")
                .Add("Pools")
                .Add("Rej%")
                .Add("Stale%")
                .Add("HFan")
                .Add("Fans")
                .Add("HTemp")
                .Add("Temps")
                .Add("Freq")
                .Add("XCount")
                .Add("Status")
            End With
        End With

        With Me.dataAnts
            .Columns(0).Width = 59
            .Columns(1).Width = 66
            .Columns(2).Width = 87
            .Columns(3).Width = 96
            .Columns(4).Width = 66
            .Columns(5).Width = 85
            .Columns(6).Width = 99
            .Columns(7).Width = 59
            .Columns(8).Width = 65
            .Columns(9).Width = 65
            .Columns(10).Width = 56
            .Columns(11).Width = 169
            .Columns(12).Width = 70
            .Columns(13).Width = 239
            .Columns(14).Width = 50
            .Columns(15).Width = 73
            .Columns(16).Width = 260
        End With

        ctlsByKey = New ControlsByRegistry(csRegKey)

        Call SetGridSizes("\Columns\dataAnts", Me.dataAnts)

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey)
            If key Is Nothing Then
                My.Computer.Registry.CurrentUser.CreateSubKey(csRegKey)
            End If
        End Using

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey)
            If key.GetValue("Width") > 100 Then
                Me.Width = key.GetValue("Width")
            End If

            If key.GetValue("Height") > 100 Then
                Me.Height = key.GetValue("Height")
            End If
        End Using

        With ctlsByKey
            .AddControl(Me.chkWBRebootIfXd, "RebootAntIfXd")
            .AddControl(Me.txtWBPassword, "Password")
            .AddControl(Me.txtWBUserName, "Username")
            .AddControl(Me.chkWBSavePassword, "SavePassword")
            .AddControl(Me.chklstAnts, "AntList")
            .AddControl(Me.txtRefreshRate, "RefreshRateValue")
            .AddControl(Me.cmbRefreshRate, "RefreshRateVolume")
            .AddControl(Me.chkShowBestShare, "ShowBestShare")
            .AddControl(Me.chkShowBlocks, "ShowBlocks")
            .AddControl(Me.chkShowFans, "ShowFans")
            .AddControl(Me.chkShowGHs5s, "ShowGHs5s")
            .AddControl(Me.chkShowGHsAvg, "ShowGHsAvg")
            .AddControl(Me.chkShowHWE, "ShowHWE")
            .AddControl(Me.chkShowPools, "ShowPools")
            .AddControl(Me.chkShowStatus, "ShowStatus")
            .AddControl(Me.chkShowTemps, "ShowTemps")
            .AddControl(Me.chkShowUptime, "ShowUptime")
            .AddControl(Me.chkShowFreqs, "ShowFreqs")
            .AddControl(Me.chkShowHighFan, "ShowHighFan")
            .AddControl(Me.chkShowHighTemp, "ShowHighTemp")
            .AddControl(Me.chkShowXCount, "ShowXCount")
            .AddControl(Me.chkShowRej, "ShowReject")
            .AddControl(Me.chkShowStale, "ShowStale")
            .AddControl(Me.chkUseAPI, "UseAPI")

            .SetControlByRegKey(Me.chkWBRebootIfXd, True)
            .SetControlByRegKey(Me.txtWBPassword, "root")
            .SetControlByRegKey(Me.txtWBUserName, "root")
            .SetControlByRegKey(Me.chkWBSavePassword, True)

            'change unmarked S1s to S1s
            Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & "\" & Me.chklstAnts.Name)
                If key Is Nothing Then
                    My.Computer.Registry.CurrentUser.CreateSubKey(csRegKey & "\" & Me.chklstAnts.Name)
                End If
            End Using

            Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & "\" & Me.chklstAnts.Name, True)
                Dim sTemp, sValue As String

                For Each sTemp In key.GetValueNames
                    sValue = key.GetValue(sTemp)

                    If sValue.Substring(0, 2) <> "S1" AndAlso sValue.Substring(0, 2) <> "S2" Then
                        key.SetValue(sTemp, "S1: " & sTemp)
                    End If
                Next
            End Using

            .SetControlByRegKey(Me.chklstAnts)
            .SetControlByRegKey(Me.txtRefreshRate, "300")
            .SetControlByRegKey(Me.cmbRefreshRate, "Seconds")
            .SetControlByRegKey(Me.chkShowBestShare, True)
            .SetControlByRegKey(Me.chkShowBlocks, True)
            .SetControlByRegKey(Me.chkShowFans, True)
            .SetControlByRegKey(Me.chkShowGHs5s, True)
            .SetControlByRegKey(Me.chkShowGHsAvg, True)
            .SetControlByRegKey(Me.chkShowHWE, True)
            .SetControlByRegKey(Me.chkShowPools, True)
            .SetControlByRegKey(Me.chkShowStatus, True)
            .SetControlByRegKey(Me.chkShowTemps, True)
            .SetControlByRegKey(Me.chkShowUptime, True)
            .SetControlByRegKey(Me.chkShowFreqs, True)
            .SetControlByRegKey(Me.chkShowHighTemp, True)
            .SetControlByRegKey(Me.chkShowHighFan, True)
            .SetControlByRegKey(Me.chkShowXCount, True)
            .SetControlByRegKey(Me.chkShowRej, True)
            .SetControlByRegKey(Me.chkShowStale, True)

            .SetControlByRegKey(Me.chkUseAPI, True)
        End With

        'check each of the boxes
        For x As Integer = 0 To Me.chklstAnts.Items.Count - 1
            Me.chklstAnts.SetItemChecked(x, True)
        Next

        Call CalcRefreshRate()

        wb(0) = New WebBrowser
        AddHandler wb(0).DocumentCompleted, AddressOf Me.wb_completed

        wb(1) = New WebBrowser
        AddHandler wb(1).DocumentCompleted, AddressOf Me.wb_completed

        wb(2) = New WebBrowser
        AddHandler wb(2).DocumentCompleted, AddressOf Me.wb_completed

        Call RefreshGrid()

    End Sub

    Private Sub wb_completed(sender As Object, e As System.Windows.Forms.WebBrowserDocumentCompletedEventArgs)

        Dim dr As DataRow
        Dim x, y, z As Integer
        Dim sAnt As String
        Dim bAntFound As Boolean
        Dim wb As WebBrowser
        Dim sbTemp As System.Text.StringBuilder
        Dim count(0 To 9) As Integer
        Dim dReboot As Date
        Dim bRebooting As Boolean

        wb = sender

        'Select Case wb.Name
        '    Case "WebBrowser1"
        '        Me.lblBrowser1.Text = wb.Url.AbsoluteUri

        '    Case "WebBrowser2"
        '        Me.lblBrowser2.Text = wb.Url.AbsoluteUri

        '    Case "WebBrowser3"
        '        Me.lblBrowser3.Text = wb.Url.AbsoluteUri

        'End Select

        sbTemp = New System.Text.StringBuilder

        'first slash slash
        x = InStr(wb.Url.AbsoluteUri, "/")

        'second slash, should be after address
        x = InStr(x + 2, wb.Url.AbsoluteUri, "/")

        While y < x
            z = InStr(y + 1, wb.Url.AbsoluteUri.Substring(0, x), ".")

            If z = 0 Then
                If y = 0 Then
                    y = InStr(wb.Url.AbsoluteUri, "//") + 1
                End If

                Exit While
            Else
                y = z
            End If
        End While

        sAnt = wb.Url.AbsoluteUri.Substring(y, x - y - 1)

        If wb.Document.All(1).OuterHtml.ToLower.Contains("authorization") Then
            AddToLog(sAnt & " responded with login page")

            wb.Document.All("username").SetAttribute("value", Me.txtWBUserName.Text)
            wb.Document.All("password").SetAttribute("value", Me.txtWBPassword.Text)
            wb.Document.All(48).InvokeMember("click")
        Else
            For Each dr In ds.Tables(0).Rows
                If dr.Item("Name") = sAnt Then
                    bAntFound = True

                    Exit For
                End If
            Next

            If bAntFound = False Then
                dr = ds.Tables(0).NewRow
            End If

            dr.Item("Name") = sAnt

#If DEBUG Then
            For x = 0 To wb.Document.All.Count - 1
                Debug.Print(x & " -- " & wb.Document.All(x).OuterText)
                Debug.Print(x & " -- " & wb.Document.All(x).OuterHtml)
            Next
#End If
            If wb.Url.AbsoluteUri.Contains("minerStatus.cgi") Then
                'S2 status code
                AddToLog(wb.Url.AbsoluteUri & " responded with status page")

                dr.Item("Uptime") = wb.Document.All(88).OuterText
                dr.Item("GH/s(5s)") = wb.Document.All(91).OuterText
                dr.Item("GH/s(avg)") = wb.Document.All(94).OuterText
                dr.Item("Blocks") = wb.Document.All(97).OuterText
                dr.Item("HWE%") = Format(UInt64.Parse(wb.Document.All(109).OuterText) / _
                        (UInt64.Parse(wb.Document.All(127).OuterText) + UInt64.Parse(wb.Document.All(130).OuterText) + UInt64.Parse(wb.Document.All(109).OuterText)), "##0.###%")
                dr.Item("BestShare") = Format(UInt64.Parse(wb.Document.All(137).OuterText), "###,###,###,###,###,##0")

                Select Case wb.Document.All(193).OuterText
                    Case "Alive"
                        sbTemp.Append("U")

                    Case "Dead"
                        sbTemp.Append("D")

                End Select

                If wb.Document.All.Count > 224 Then
                    Select Case wb.Document.All(245).OuterText
                        Case "Alive"
                            sbTemp.Append("U")

                        Case "Dead"
                            sbTemp.Append("D")

                    End Select

                    Select Case wb.Document.All(297).OuterText
                        Case "Alive"
                            sbTemp.Append("U")

                        Case "Dead"
                            sbTemp.Append("D")

                    End Select

                    dr.Item("Pools") = sbTemp.ToString

                    sbTemp.Clear()

                    dr.Item("HFan") = GetHighValue(wb.Document.All(530).OuterText, wb.Document.All(531).OuterText, wb.Document.All(532).OuterText, wb.Document.All(533).OuterText)

                    dr.Item("Fans") = wb.Document.All(530).OuterText & " " & wb.Document.All(531).OuterText & " " & wb.Document.All(532).OuterText & " " & wb.Document.All(533).OuterText

                    dr.Item("Freq") = wb.Document.All(366).OuterText & " " & wb.Document.All(382).OuterText & " " & wb.Document.All(398).OuterText & " " & wb.Document.All(414).OuterText & " " & _
                                       wb.Document.All(430).OuterText & " " & wb.Document.All(446).OuterText & " " & wb.Document.All(462).OuterText & " " & wb.Document.All(478).OuterText & " " & _
                                       wb.Document.All(494).OuterText & " " & wb.Document.All(510).OuterText

                    dr.Item("HTemp") = GetHighValue(wb.Document.All(369).OuterText, wb.Document.All(385).OuterText, wb.Document.All(401).OuterText, wb.Document.All(417).OuterText, _
                                                    wb.Document.All(433).OuterText, wb.Document.All(449).OuterText, wb.Document.All(465).OuterText, wb.Document.All(481).OuterText, _
                                                    wb.Document.All(497).OuterText, wb.Document.All(513).OuterText)

                    dr.Item("Temps") = wb.Document.All(369).OuterText & " " & wb.Document.All(385).OuterText & " " & wb.Document.All(401).OuterText & " " & wb.Document.All(417).OuterText & " " & _
                                       wb.Document.All(433).OuterText & " " & wb.Document.All(449).OuterText & " " & wb.Document.All(465).OuterText & " " & wb.Document.All(481).OuterText & " " & _
                                       wb.Document.All(497).OuterText & " " & wb.Document.All(513).OuterText

                    count(0) = HowManyInString(wb.Document.All(372).OuterText, "x") + HowManyInString(wb.Document.All(372).OuterText, "-")
                    count(1) = HowManyInString(wb.Document.All(388).OuterText, "x") + HowManyInString(wb.Document.All(388).OuterText, "-")
                    count(2) = HowManyInString(wb.Document.All(404).OuterText, "x") + HowManyInString(wb.Document.All(404).OuterText, "-")
                    count(3) = HowManyInString(wb.Document.All(420).OuterText, "x") + HowManyInString(wb.Document.All(420).OuterText, "-")
                    count(4) = HowManyInString(wb.Document.All(436).OuterText, "x") + HowManyInString(wb.Document.All(436).OuterText, "-")
                    count(5) = HowManyInString(wb.Document.All(452).OuterText, "x") + HowManyInString(wb.Document.All(452).OuterText, "-")
                    count(6) = HowManyInString(wb.Document.All(468).OuterText, "x") + HowManyInString(wb.Document.All(468).OuterText, "-")
                    count(7) = HowManyInString(wb.Document.All(484).OuterText, "x") + HowManyInString(wb.Document.All(484).OuterText, "-")
                    count(8) = HowManyInString(wb.Document.All(500).OuterText, "x") + HowManyInString(wb.Document.All(500).OuterText, "-")
                    count(9) = HowManyInString(wb.Document.All(516).OuterText, "x") + HowManyInString(wb.Document.All(516).OuterText, "-")

                    dr.Item("XCount") = count(0) + count(1) + count(2) + count(3) + count(4) + count(5) + count(6) + count(7) + count(8) + count(9) & "X"

                    dr.Item("Status") = count(0) & "X " & count(1) & "X " & count(2) & "X " & count(3) & "X " & count(4) & "X " & count(5) & "X " & _
                                        count(6) & "X " & count(7) & "X " & count(8) & "X " & count(9) & "X"

                    If (count(0) <> 0 OrElse count(1) <> 0 OrElse count(2) <> 0 OrElse count(3) <> 0 OrElse count(4) <> 0 OrElse count(5) <> 0 _
                        OrElse count(6) <> 0 OrElse count(7) <> 0 OrElse count(8) <> 0 OrElse count(9) <> 0) AndAlso Me.chkWBRebootIfXd.Checked = True Then
                        'only reboot once every 15 minutes
                        If RebootInfo.TryGetValue(sAnt, dReboot) = True Then
                            If dReboot.AddMinutes(15) < Now Then
                                bRebooting = True

                                RebootInfo.Remove(sAnt)
                            Else
                                AddToLog("Need to reboot " & dr.Item("Name") & ", but hasn't been 15 minutes")
                            End If
                        Else
                            bRebooting = True
                        End If

                        If bRebooting = True Then
                            AddToLog("REBOOTING " & dr.Item("Name"))

                            RebootInfo.Add(sAnt, Now)

                            wb.Navigate("http://192.168.0." & dr.Item("Name") & "/reboot.html")
                        End If
                    Else
                        If Me.TimerRefresh.Enabled = False Then
                            Call RefreshGrid()
                        End If
                    End If
                End If

            ElseIf wb.Url.AbsoluteUri.Contains("/reboot.html") = True Then
                'S2 reboot
                wb.Document.All(66).InvokeMember("click")

            ElseIf wb.Url.AbsoluteUri.Contains("/admin/status/minerstatus/") = True Then
                'S1 status code    
                AddToLog(wb.Url.AbsoluteUri & " responded with status page")

                If wb.Url.AbsoluteUri.Contains("minerstatus") AndAlso wb.Document.All.Count > 75 Then
                    dr.Item("Uptime") = wb.Document.All(122).OuterText.TrimEnd

                    If wb.Document.All(84).Children(2).Children.Count <> 1 Then
                        dr.Item("GH/s(5s)") = wb.Document.All(126).OuterText.TrimEnd
                        dr.Item("GH/s(avg)") = wb.Document.All(130).OuterText.TrimEnd
                        dr.Item("Blocks") = wb.Document.All(134).OuterText.TrimEnd
                        dr.Item("HWE%") = Format(UInt64.Parse(wb.Document.All(150).OuterText.TrimEnd.Replace(",", "")) / _
                                         (UInt64.Parse(wb.Document.All(174).OuterText.TrimEnd.Replace(",", "")) + _
                                          UInt64.Parse(wb.Document.All(178).OuterText.TrimEnd.Replace(",", "")) + _
                                          UInt64.Parse(wb.Document.All(150).OuterText.TrimEnd.Replace(",", ""))), "##0.###%")
                        dr.Item("BestShare") = wb.Document.All(186).OuterText.TrimEnd

                        Select Case wb.Document.All(247).OuterText.TrimEnd
                            Case "Alive"
                                sbTemp.Append("U")

                            Case "Dead"
                                sbTemp.Append("D")

                        End Select

                        'dr.Item("P0Status") = wb.Document.All(192).Children(2).Children(0).Children(0).Children(2).Children(3).Children(0).OuterText.TrimEnd

                        If wb.Document.All(192).Children(2).Children(0).Children(0).Children.Count > 3 Then
                            'dr.Item("P1Status") = wb.Document.All(192).Children(2).Children(0).Children(0).Children(3).Children(3).Children(0).OuterText.TrimEnd

                            Select Case wb.Document.All(192).Children(2).Children(0).Children(0).Children(3).Children(3).Children(0).OuterText.TrimEnd
                                Case "Alive"
                                    sbTemp.Append("U")

                                Case "Dead"
                                    sbTemp.Append("D")

                            End Select

                            If wb.Document.All(192).Children(2).Children(0).Children(0).Children.Count > 4 Then
                                'dr.Item("P2Status") = wb.Document.All(192).Children(2).Children(0).Children(0).Children(4).Children(3).Children(0).OuterText.TrimEnd

                                Select Case wb.Document.All(192).Children(2).Children(0).Children(0).Children(4).Children(3).Children(0).OuterText.TrimEnd
                                    Case "Alive"
                                        sbTemp.Append("U")

                                    Case "Dead"
                                        sbTemp.Append("D")

                                End Select

                                x = 443
                            Else
                                'dr.Item("P2Status") = "N/A"
                                sbTemp.Append("N")

                                x = 374
                            End If
                        Else
                            'dr.Item("P1Status") = "N/A"
                            sbTemp.Append("NN")

                            x = 305
                        End If
                        dr.Item("Pools") = sbTemp.ToString

                        sbTemp.Clear()

                        dr.Item("HFan") = GetHighValue(wb.Document.All(x + 33).OuterText.TrimEnd, wb.Document.All(x + 58).OuterText.TrimEnd)

                        dr.Item("Fans") = wb.Document.All(x + 33).OuterText.TrimEnd & " " & _
                                          wb.Document.All(x + 58).OuterText.TrimEnd

                        dr.Item("HTemp") = GetHighValue(wb.Document.All(x + 37).OuterText.TrimEnd, wb.Document.All(x + 62).OuterText.TrimEnd)

                        dr.Item("Temps") = wb.Document.All(x + 37).OuterText.TrimEnd & " " & _
                                           wb.Document.All(x + 62).OuterText.TrimEnd

                        dr.Item("Freq") = wb.Document.All(x + 29).OuterText.TrimEnd & " " & wb.Document.All(x + 54).OuterText.TrimEnd

                        count(0) = HowManyInString(wb.Document.All(x + 41).OuterText.TrimEnd, "x")
                        count(1) = HowManyInString(wb.Document.All(x + 66).OuterText.TrimEnd, "x")

                        dr.Item("XCount") = count(0) + count(1) & "X"

                        dr.Item("Status") = count(0) & "X " & count(1) & "X"
                    End If

                    If (count(0) <> 0 OrElse count(1) <> 0) AndAlso Me.chkWBRebootIfXd.Checked = True Then
                        If RebootInfo.TryGetValue(sAnt, dReboot) = True Then
                            If dReboot.AddMinutes(15) < Now Then
                                bRebooting = True

                                RebootInfo.Remove(sAnt)
                            Else
                                AddToLog("Need to reboot " & dr.Item("Name") & ", but hasn't been 15 minutes")
                            End If
                        Else
                            bRebooting = True
                        End If

                        If bRebooting = True Then
                            AddToLog("REBOOTING " & dr.Item("Name"))

                            RebootInfo.Add(sAnt, Now)

                            wb.Navigate("http://192.168.0." & dr.Item("Name") & "/cgi-bin/luci/;stok=/admin/system/reboot?reboot=1")
                        End If
                    Else
                        If Me.TimerRefresh.Enabled = False Then
                            Call RefreshGrid()
                        End If
                    End If
                End If
            End If

            If bAntFound = False Then
                ds.Tables(0).Rows.Add(dr)
            End If

            Me.dataAnts.Refresh()
        End If

    End Sub

    Private Function GetHighValue(ByVal s1 As String, ByVal s2 As String, Optional ByVal s3 As String = "", Optional ByVal s4 As String = "", Optional ByVal s5 As String = "", _
                                  Optional ByVal s6 As String = "", Optional ByVal s7 As String = "", Optional ByVal s8 As String = "", Optional ByVal s9 As String = "", _
                                  Optional ByVal s10 As String = "") As Integer

        Dim h As Integer

        If Val(s1) > Val(s2) Then
            h = Val(s1)
        Else
            h = Val(s2)
        End If

        If s3.IsNullOrEmpty = False Then
            If Val(s3) > h Then
                h = Val(s3)
            End If

            If s4.IsNullOrEmpty = False Then
                If Val(s4) > h Then
                    h = Val(s4)
                End If

                If s5.IsNullOrEmpty = False Then
                    If Val(s5) > h Then
                        h = Val(s5)
                    End If

                    If s6.IsNullOrEmpty = False Then
                        If Val(s6) > h Then
                            h = Val(s6)
                        End If

                        If s7.IsNullOrEmpty = False Then
                            If Val(s7) > h Then
                                h = Val(s7)
                            End If

                            If s8.IsNullOrEmpty = False Then
                                If Val(s8) > h Then
                                    h = Val(s8)
                                End If

                                If s9.IsNullOrEmpty = False Then
                                    If Val(s9) > h Then
                                        h = Val(s9)
                                    End If

                                    If s10.IsNullOrEmpty = False Then
                                        If Val(s10) > h Then
                                            h = Val(s10)
                                        End If
                                    End If
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        End If

        Return h

    End Function


    Private Function HowManyInString(ByVal sString As String, sSearch As String) As Integer

        Dim i, x As Integer

        For x = 0 To sString.Length - 1
            If sString.Substring(x, 1).ToLower = sSearch.ToLower Then
                i += 1
            End If
        Next

        Return i

    End Function

    Private Sub TimerRefresh_Tick(sender As Object, e As System.EventArgs) Handles TimerRefresh.Tick

        iCountDown -= 1

        If iCountDown < 0 Then
            iCountDown = iRefreshRate
        End If

        If iCountDown = 0 Then
            Me.TimerRefresh.Enabled = False
            Me.cmdPause.Enabled = False

            iWatchDog = 300 '5 minutes

            If Me.chkUseAPI.Checked = False Then
                Me.TimerWatchdog.Enabled = True
            End If

            'clear the uptime column to indicate we're refreshing
            For Each dr As DataRow In Me.ds.Tables(0).Rows
                dr.Item("UpTime") = "???"
            Next

            Me.dataAnts.Refresh()

            AddToLog("Initiated refresh")

            Call RefreshGrid()

            iCountDown = iRefreshRate
        End If

        Me.cmdRefresh.Text = "Refreshing in " & iCountDown

    End Sub

    Private Sub cmdRefresh_Click(sender As System.Object, e As System.EventArgs) Handles cmdRefresh.Click

        iCountDown = 1

        Call TimerRefresh_Tick(sender, e)

    End Sub

    Private Function GetHeader() As String

        Return "Authorization: Basic " & Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes(Me.txtWBUserName.Text & ":" & Me.txtWBPassword.Text)) & System.Environment.NewLine

    End Function

    Private Sub RefreshGrid()

        Dim sTemp, sAntIP, sAnt As String
        Dim bAntFound As Boolean
        Dim dr As DataRow
        Dim j, jp1 As Newtonsoft.Json.Linq.JObject
        Dim ja As Newtonsoft.Json.Linq.JArray
        Dim ts As TimeSpan
        Dim sbTemp As System.Text.StringBuilder
        Dim count(0 To 9), iTemp As Integer
        Dim sResult As String
        Dim dBestShare As Double
        Dim AntType As enAntType

        If Me.chklstAnts.Items.Count = 0 Then
            MsgBox("Please add some Ant addresses first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

            Me.TabControl1.SelectTab(1)

            Exit Sub
        End If

        sbTemp = New System.Text.StringBuilder

        If Me.chkUseAPI.Checked = True Then
            While bAnt <> Me.chklstAnts.CheckedItems.Count
                sbTemp.Clear()

                Select Case Me.chklstAnts.CheckedItems(bAnt).ToString.Substring(0, 2)
                    Case "S2"
                        AntType = enAntType.S2

                    Case "S1"
                        AntType = enAntType.S1

                    Case Else
                        Throw New Exception("Unknown ant type.")

                End Select

                sAntIP = Me.chklstAnts.CheckedItems(bAnt).ToString.Substring(4)

                sAnt = sAntIP.InstrRev(".")

                For Each dr In ds.Tables(0).Rows
                    If dr.Item("Name") = sAnt Then
                        bAntFound = True

                        Exit For
                    End If
                Next

                If bAntFound = False Then
                    dr = ds.Tables(0).NewRow
                End If

                dr.Item("Name") = sAnt

                Try
                    sResult = GetIPData(sAntIP, "stats")

                    j = Newtonsoft.Json.Linq.JObject.Parse(sResult)

                    For Each ja In j.Property("STATS")
                        For Each jp1 In ja
                            ts = New TimeSpan(0, 0, jp1.Value(Of Integer)("Elapsed"))

                            dr.Item("Uptime") = Format(ts.Days, "0d") & " " & Format(ts.Hours, "0h") & " " & Format(ts.Minutes, "0m") & " " & Format(ts.Seconds, "0s")
                            dr.Item("HWE%") = jp1.Value(Of String)("Device Hardware%") & "%"

                            dr.Item("HFan") = GetHighValue(jp1.Value(Of Integer)("fan1"), jp1.Value(Of Integer)("fan2"), jp1.Value(Of Integer)("fan3"), jp1.Value(Of Integer)("fan4"))

                            sbTemp.Clear()

                            For x = 1 To jp1.Value(Of Integer)("fan_num")
                                sbTemp.Append(jp1.Value(Of Integer)("fan" & x))

                                If x <> jp1.Value(Of Integer)("fan_num") Then
                                    sbTemp.Append(" ")
                                End If
                            Next

                            dr.Item("Fans") = sbTemp.ToString

                            sbTemp.Clear()

                            iTemp = 0

                            For x = 1 To jp1.Value(Of Integer)("temp_num")
                                sbTemp.Append(jp1.Value(Of Integer)("temp" & x))

                                If jp1.Value(Of Integer)("temp" & x) > iTemp Then
                                    iTemp = jp1.Value(Of Integer)("temp" & x)
                                End If

                                If x <> jp1.Value(Of Integer)("temp_num") Then
                                    sbTemp.Append(" ")
                                End If
                            Next

                            dr.Item("HTemp") = iTemp

                            dr.Item("Temps") = sbTemp.ToString

                            dr.Item("Freq") = jp1.Value(Of String)("frequency")

                            count(0) = HowManyInString(jp1.Value(Of String)("chain_acs1"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs1"), "x")
                            count(1) = HowManyInString(jp1.Value(Of String)("chain_acs2"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs2"), "x")
                            count(2) = HowManyInString(jp1.Value(Of String)("chain_acs3"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs3"), "x")
                            count(3) = HowManyInString(jp1.Value(Of String)("chain_acs4"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs4"), "x")

                            If AntType = enAntType.S2 Then
                                count(4) = HowManyInString(jp1.Value(Of String)("chain_acs5"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs5"), "x")
                                count(5) = HowManyInString(jp1.Value(Of String)("chain_acs6"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs6"), "x")
                                count(6) = HowManyInString(jp1.Value(Of String)("chain_acs7"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs7"), "x")
                                count(7) = HowManyInString(jp1.Value(Of String)("chain_acs8"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs8"), "x")
                                count(8) = HowManyInString(jp1.Value(Of String)("chain_acs9"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs9"), "x")
                                count(9) = HowManyInString(jp1.Value(Of String)("chain_acs10"), "-") + HowManyInString(jp1.Value(Of String)("chain_acs10"), "x")
                            Else
                                count(4) = 0
                                count(5) = 0
                                count(6) = 0
                                count(7) = 0
                                count(8) = 0
                                count(9) = 0
                            End If

                            dr.Item("XCount") = count(0) + count(1) + count(2) + count(3) + count(4) + count(5) + count(6) + count(7) + count(8) + count(9) & "X"

                            If AntType = enAntType.S2 Then
                                dr.Item("Status") = count(0) & "X " & count(1) & "X " & count(2) & "X " & count(3) & "X " & count(4) & "X " & count(5) & "X " & _
                                                    count(6) & "X " & count(7) & "X " & count(8) & "X " & count(9) & "X"
                            Else
                                dr.Item("Status") = count(0) & "X " & count(1) & "X "
                            End If

                            Exit For
                        Next

                        Exit For
                    Next

                    sResult = GetIPData(sAntIP, "summary")

                    j = Newtonsoft.Json.Linq.JObject.Parse(sResult)

                    For Each ja In j.Property("SUMMARY")
                        For Each jp1 In ja
                            dr.Item("GH/s(5s)") = jp1.Value(Of String)("GHS 5s")
                            dr.Item("GH/s(avg)") = jp1.Value(Of String)("GHS av")

                            dr.Item("Rej%") = jp1.Value(Of String)("Pool Rejected%")
                            dr.Item("Stale%") = jp1.Value(Of String)("Pool Stale%")

                            dr.Item("Blocks") = jp1.Value(Of String)("Found Blocks")
                        Next
                    Next

                    sResult = GetIPData(sAntIP, "pools")

                    j = Newtonsoft.Json.Linq.JObject.Parse(sResult)

                    dBestShare = 0

                    sbTemp.Clear()

                    For Each ja In j.Property("POOLS")
                        For Each jp1 In ja
                            If jp1.Value(Of Double)("Best Share") > dBestShare Then
                                dBestShare = jp1.Value(Of Double)("Best Share")
                            End If

                            Select Case jp1.Value(Of String)("Status")
                                Case "Alive"
                                    sbTemp.Append("U")

                                Case "Dead"
                                    sbTemp.Append("D")

                                Case Else
                                    sbTemp.Append("U")

                            End Select
                        Next

                        Exit For
                    Next

                    dr.Item("BestShare") = Format(dBestShare, "###,###,###,###,###,##0")
                    dr.Item("Pools") = sbTemp.ToString
                Catch ex As Exception
                    dr.Item("Uptime") = "ERROR"
                End Try

                If bAntFound = False Then
                    ds.Tables(0).Rows.Add(dr)
                End If

                Me.dataAnts.Refresh()

                bAnt += 1
            End While

            Me.cmdPause.Enabled = True
            Me.TimerRefresh.Enabled = True
            Me.TimerWatchdog.Enabled = False
            bAnt = 0

            Me.Text = csVersion & " - Refreshed " & Now.ToString

            Exit Sub
        Else
            If bAnt <> Me.chklstAnts.CheckedItems.Count Then
                sTemp = Me.chklstAnts.CheckedItems(bAnt).ToString

                If wb(0).IsBusy = False Then
                    AddToLog("Submitting " & Me.chklstAnts.CheckedItems(bAnt) & " on instance 0")

                    'sock.Connect("192.168.0.91", 4028)

                    If sTemp.Substring(0, 2) = "S2" Then
                        wb(0).Navigate(String.Format("http://{0}:{1}@" & sTemp.Substring(4) & "/cgi-bin/minerStatus.cgi", Me.txtWBUserName.Text, Me.txtWBPassword.Text), Nothing, Nothing, GetHeader)
                    Else
                        wb(0).Navigate("http://" & sTemp.Substring(4) & "/cgi-bin/luci/;stok=/admin/status/minerstatus/", False)
                    End If

                    bAnt += 1
                End If
            End If

            If bAnt <> Me.chklstAnts.CheckedItems.Count Then
                sTemp = Me.chklstAnts.CheckedItems(bAnt).ToString

                If wb(1).IsBusy = False Then
                    AddToLog("Submitting " & Me.chklstAnts.CheckedItems(bAnt) & " on instance 1")

                    If sTemp.Substring(0, 2) = "S2" Then
                        wb(1).Navigate(String.Format("http://{0}:{1}@" & sTemp.Substring(4) & "/cgi-bin/minerStatus.cgi", Me.txtWBUserName.Text, Me.txtWBPassword.Text), Nothing, Nothing, GetHeader)
                    Else
                        wb(1).Navigate("http://" & sTemp.Substring(4) & "/cgi-bin/luci/;stok=/admin/status/minerstatus/", False)
                    End If

                    bAnt += 1
                End If
            End If

            If bAnt <> Me.chklstAnts.CheckedItems.Count Then
                sTemp = Me.chklstAnts.CheckedItems(bAnt).ToString

                If wb(2).IsBusy = False Then
                    AddToLog("Submitting " & Me.chklstAnts.CheckedItems(bAnt) & " on instance 2")

                    If sTemp.Substring(0, 2) = "S2" Then
                        wb(2).Navigate(String.Format("http://{0}:{1}@" & sTemp.Substring(4) & "/cgi-bin/minerStatus.cgi", Me.txtWBUserName.Text, Me.txtWBPassword.Text), Nothing, Nothing, GetHeader)
                    Else
                        wb(2).Navigate("http://" & sTemp.Substring(4) & "/cgi-bin/luci/;stok=/admin/status/minerstatus/", False)
                    End If

                    bAnt += 1
                End If
            End If

            If bAnt = Me.chklstAnts.CheckedItems.Count Then
                Me.cmdPause.Enabled = True
                Me.TimerRefresh.Enabled = True
                Me.TimerWatchdog.Enabled = False
                bAnt = 0

                Me.Text = csVersion & " - Refreshed " & Now.ToString
            End If
        End If

    End Sub

    Private Function GetIPData(ByVal sIP As String, ByVal sCommand As String) As String

        Dim socket As System.Net.Sockets.TcpClient
        Dim s As System.IO.Stream
        Dim b() As Byte
        Dim sbTemp As System.Text.StringBuilder

        socket = New System.Net.Sockets.TcpClient
        socket.Connect(sIP, "4028")
        s = socket.GetStream

        b = System.Text.Encoding.ASCII.GetBytes("{""command"":""" & sCommand & """}" & vbCrLf)

        s.Write(b, 0, b.Length)

        sbTemp = New System.Text.StringBuilder

        While sbTemp.Length < 2 OrElse sbTemp.ToString.Substring(sbTemp.Length - 2, 1) <> "}"
            My.Application.DoEvents()
            System.Threading.Thread.Sleep(100)

            If socket.Available <> 0 Then
                Array.Resize(b, socket.Available)
                s.Read(b, 0, b.Length)

                sbTemp.Append(System.Text.Encoding.ASCII.GetString(b))
            End If
        End While

        s.Close()
        socket.Close()

        Return sbTemp.ToString

    End Function

    Private Sub dataGrid_ColumnWidthChanged(sender As Object, e As System.Windows.Forms.DataGridViewColumnEventArgs) Handles dataAnts.ColumnWidthChanged

        Dim dt As DataGridView

        dt = DirectCast(sender, DataGridView)

        With My.Computer.Registry
            .CurrentUser.CreateSubKey(csRegKey & "\Columns\" & dt.Name)
            .SetValue("HKEY_CURRENT_USER\" & csRegKey & "\Columns\" & dt.Name, e.Column.Name, e.Column.Width, Microsoft.Win32.RegistryValueKind.DWord)
        End With

    End Sub

    Private Sub SetGridSizes(ByVal sKey As String, ByRef dataGrid As DataGridView)

        Using key As Microsoft.Win32.RegistryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(csRegKey & sKey)
            If key IsNot Nothing Then
                For Each colAny As DataGridViewColumn In dataGrid.Columns
                    If key.GetValue(colAny.Name) <> 0 Then
                        colAny.Width = key.GetValue(colAny.Name)
                    End If
                Next

                key.Close()
            End If
        End Using

    End Sub

    Private Sub frmMain_ResizeEnd(sender As Object, e As System.EventArgs) Handles Me.ResizeEnd

        With My.Computer.Registry
            .CurrentUser.CreateSubKey(csRegKey)
            .SetValue("HKEY_CURRENT_USER\" & csRegKey, "Width", Me.Width, Microsoft.Win32.RegistryValueKind.DWord)
            .SetValue("HKEY_CURRENT_USER\" & csRegKey, "Height", Me.Height, Microsoft.Win32.RegistryValueKind.DWord)
        End With

    End Sub

    Private Sub cmdScan_Click(sender As System.Object, e As System.EventArgs) Handles cmdScan.Click

        Dim sResponse, sLocalNet As String
        Dim x As Integer
        Dim wc As eWebClient

        Static bStopRequested As Boolean

        Try
            If Me.cmdScan.Text = "Scan" Then
                If Me.cmbLocalIPs.Text.IsNullOrEmpty = True Then
                    MsgBox("Please select your local IP address first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

                    Me.cmbLocalIPs.DroppedDown = True

                    Exit Sub
                End If

                If Me.txtWBUserName.Text.IsNullOrEmpty = True OrElse Me.txtWBPassword.Text.IsNullOrEmpty = True Then
                    MsgBox("Please enter your Ant credentials first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

                    Me.txtWBUserName.Focus()

                    Exit Sub
                End If

                sLocalNet = Me.cmbLocalIPs.Text.Substring(0, Microsoft.VisualBasic.InStrRev(Me.cmbLocalIPs.Text, "."))

                wc = New eWebClient
                wc.Credentials = New System.Net.NetworkCredential(Me.txtWBUserName.Text, Me.txtWBPassword.Text)

                Me.cmdScan.Text = "STOP!"

                Me.ProgressBar1.Minimum = 1
                Me.ProgressBar1.Maximum = 255
                Me.ProgressBar1.Visible = True
                Me.lblScanning.Visible = True
                My.Application.DoEvents()

                For x = 1 To 255
                    If bStopRequested = True Then
                        bStopRequested = False

                        Me.cmdScan.Text = "Scan"

                        Exit For
                    End If

                    Me.ProgressBar1.Value = x
                    Me.ToolTip1.SetToolTip(Me.ProgressBar1, sLocalNet & x.ToString)

                    If sLocalNet & x.ToString <> Me.cmbLocalIPs.Text Then
                        Try
                            Debug.Print(x)

                            My.Application.DoEvents()

                            sResponse = wc.DownloadString("http://" & sLocalNet & x.ToString)

                            If sResponse.Contains("href=""/cgi-bin/luci"">LuCI - Lua Configuration Interface</a>") = True Then
                                wc.DownloadFile("http://" & sLocalNet & x.ToString & "/luci-static/resources/icons/antminer_logo.png", My.Computer.FileSystem.SpecialDirectories.Temp & "\ant.png")

                                My.Computer.FileSystem.DeleteFile(My.Computer.FileSystem.SpecialDirectories.Temp & "\ant.png")

                                Me.chklstAnts.SetItemChecked("S1: " & Me.chklstAnts.Items.Add(sLocalNet & x.ToString), True)

                                Call AddToLog("S1 found at " & sLocalNet & x.ToString & "!")

                                My.Application.DoEvents()
                            End If

                            If sResponse.Contains("<tr><td width=""33%"">Miner Type</td><td id=""ant_minertype""></td></tr>") Then
                                wc.DownloadFile("http://" & sLocalNet & x.ToString & "/images/antminer_logo.png", My.Computer.FileSystem.SpecialDirectories.Temp & "\ant.png")

                                My.Computer.FileSystem.DeleteFile(My.Computer.FileSystem.SpecialDirectories.Temp & "\ant.png")

                                Me.chklstAnts.SetItemChecked("S2: " & Me.chklstAnts.Items.Add(sLocalNet & x.ToString), True)

                                Call AddToLog("S2 found at " & sLocalNet & x.ToString & "!")

                                My.Application.DoEvents()
                            End If

                            Debug.Print(sResponse)
                        Catch ex As Exception
                        End Try
                    End If
                Next
            Else
                bStopRequested = True
            End If
        Catch ex As Exception When bErrorHandle = True
            MsgBox("The following error has occurred:" & vbCrLf & vbCrLf & ex.Message, MsgBoxStyle.Critical Or MsgBoxStyle.OkOnly)
        Finally
            Me.ToolTip1.SetToolTip(Me.ProgressBar1, "")
            Me.ProgressBar1.Visible = False
            Me.cmdScan.Enabled = True
            Me.lblScanning.Visible = False
        End Try

    End Sub

    Private Class eWebClient

        Inherits System.Net.WebClient

        Protected Overrides Function GetWebRequest(address As System.Uri) As System.Net.WebRequest
            Dim w As System.Net.WebRequest

            w = MyBase.GetWebRequest(address)
            w.Timeout = 5000

            Return w
        End Function

    End Class

    Private Sub cmdPause_Click(sender As System.Object, e As System.EventArgs) Handles cmdPause.Click

        Me.TimerRefresh.Enabled = Not Me.TimerRefresh.Enabled

        If Me.TimerRefresh.Enabled = True Then
            Me.cmdPause.Text = "Pause"
        Else
            Me.cmdPause.Text = "Resume"
        End If

    End Sub

    Private Sub cmdSaveConfig_Click(sender As System.Object, e As System.EventArgs) Handles cmdSaveConfig.Click

        With ctlsByKey
            .SetRegKeyByControl(Me.chkWBRebootIfXd)
            .SetRegKeyByControl(Me.chkWBSavePassword)

            If Me.chkWBSavePassword.Checked = True Then
                .SetRegKeyByControl(Me.txtWBPassword)
            Else
                .SetRegKeyByControl(Me.txtWBPassword, "")
            End If

            .SetRegKeyByControl(Me.txtWBUserName)
            .SetRegKeyByControl(Me.chklstAnts)

            .SetRegKeyByControl(Me.txtRefreshRate)
            .SetRegKeyByControl(Me.cmbRefreshRate)

            .SetRegKeyByControl(Me.chkShowBestShare)
            .SetRegKeyByControl(Me.chkShowBlocks)
            .SetRegKeyByControl(Me.chkShowFans)
            .SetRegKeyByControl(Me.chkShowGHs5s)
            .SetRegKeyByControl(Me.chkShowGHsAvg)
            .SetRegKeyByControl(Me.chkShowHWE)
            .SetRegKeyByControl(Me.chkShowPools)
            .SetRegKeyByControl(Me.chkShowStatus)
            .SetRegKeyByControl(Me.chkShowTemps)
            .SetRegKeyByControl(Me.chkShowUptime)
            .SetRegKeyByControl(Me.chkShowFreqs)
            .SetRegKeyByControl(Me.chkShowHighTemp)
            .SetRegKeyByControl(Me.chkShowHighFan)
            .SetRegKeyByControl(Me.chkShowXCount)
            .SetRegKeyByControl(Me.chkShowRej)
            .SetRegKeyByControl(Me.chkShowStale)

            .SetRegKeyByControl(Me.chkUseAPI)
        End With

    End Sub

    'will re-enable the normal countdown if it counts down to 0 
    'that should only happen if there are so many ants they can't be refreshed in 5 minutes
    'or something went wrong, like it's trying to reach an ant that is offline
    Private Sub TimerWatchdog_Tick(sender As Object, e As System.EventArgs) Handles TimerWatchdog.Tick

        iWatchDog -= 1

        If iWatchDog = 0 Then
            Me.TimerWatchdog.Enabled = False
            Me.TimerRefresh.Enabled = True
            Me.cmdPause.Enabled = True
        End If

    End Sub

    Private Sub cmdAddAnt_Click(sender As Object, e As System.EventArgs) Handles cmdAddAnt.Click

        Dim sTemp As String

        If Me.optAddS1.Checked = False AndAlso Me.optAddS2.Checked = False Then
            MsgBox("Please specify if this is an S1 or an S2.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

            Exit Sub
        End If

        If Me.optAddS1.Checked = True Then
            sTemp = "S1: "
        ElseIf Me.optAddS2.Checked = True Then
            sTemp = "S2: "
        End If

        If Me.txtAntAddress.Text.IsNullOrEmpty = False Then
            If Me.chklstAnts.Items.Contains(Me.txtAntAddress.Text) = False Then
                Me.chklstAnts.SetItemChecked(Me.chklstAnts.Items.Add(sTemp & Me.txtAntAddress.Text), True)
                Me.txtAntAddress.Text = ""
            Else
                MsgBox("This address appears to already be in the list.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)
            End If
        End If

    End Sub

    Private Sub cmdDelAnt_Click(sender As System.Object, e As System.EventArgs) Handles cmdDelAnt.Click

        If Me.chklstAnts.SelectedItem Is Nothing Then
            MsgBox("Please select an item to remove first.", MsgBoxStyle.Information Or MsgBoxStyle.OkOnly)

            Exit Sub
        End If

        Me.chklstAnts.Items.RemoveAt(Me.chklstAnts.SelectedIndex)

    End Sub

    Private Sub cmbRefreshRate_KeyPress(sender As Object, e As System.Windows.Forms.KeyPressEventArgs) Handles cmbRefreshRate.KeyPress

        e.Handled = True

    End Sub

    Private Sub txtRefreshRate_KeyPress(sender As Object, e As System.Windows.Forms.KeyPressEventArgs) Handles txtRefreshRate.KeyPress

        Select Case e.KeyChar
            Case "0" To "9", vbBack
                'okay

            Case Else
                e.Handled = True

        End Select

    End Sub

    Private Sub CalcRefreshRate()

        Select Case Me.cmbRefreshRate.Text
            Case "Seconds"
                iRefreshRate = Val(Me.txtRefreshRate.Text)

            Case "Minutes"
                iRefreshRate = Val(Me.txtRefreshRate.Text) * 60

            Case "Hours"
                iRefreshRate = Val(Me.txtRefreshRate.Text) * 60 * 60

        End Select

        If iRefreshRate = 0 Then
            iRefreshRate = 300
        End If

    End Sub

    Private Sub txtRefreshRate_LostFocus(sender As Object, e As System.EventArgs) Handles txtRefreshRate.LostFocus

        Call CalcRefreshRate()

    End Sub

    Private Sub cmbRefreshRate_LostFocus(sender As Object, e As System.EventArgs) Handles cmbRefreshRate.LostFocus

        Call CalcRefreshRate()

    End Sub

    Private Sub AddToLog(ByVal sText As String)

        Me.txtLog.AppendText(Now.ToLocalTime & ": " & sText & vbCrLf)

    End Sub

    Private Sub chkShow_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkShowBestShare.CheckedChanged, chkShowBlocks.CheckedChanged, _
        chkShowFans.CheckedChanged, chkShowGHs5s.CheckedChanged, chkShowGHsAvg.CheckedChanged, chkShowHWE.CheckedChanged, chkShowPools.CheckedChanged, _
        chkShowStatus.CheckedChanged, chkShowTemps.CheckedChanged, chkShowUptime.CheckedChanged, chkShowFreqs.CheckedChanged, chkShowHighFan.CheckedChanged, _
        chkShowHighTemp.CheckedChanged, chkShowXCount.CheckedChanged, chkShowRej.CheckedChanged, chkShowStale.CheckedChanged

        Dim chkAny As CheckBox

        If bStarted = False Then Exit Sub

        chkAny = DirectCast(sender, CheckBox)

        Select Case chkAny.Name
            Case "chkShowUptime"
                Me.dataAnts.Columns("Uptime").Visible = chkAny.Checked

            Case "chkShowGHs5s"
                Me.dataAnts.Columns("GH/s(5s)").Visible = chkAny.Checked

            Case "chkShowGHsAvg"
                Me.dataAnts.Columns("GH/s(avg)").Visible = chkAny.Checked

            Case "chkShowBlocks"
                Me.dataAnts.Columns("Blocks").Visible = chkAny.Checked

            Case "chkShowHWE"
                Me.dataAnts.Columns("HWE%").Visible = chkAny.Checked

            Case "chkShowBestShare"
                Me.dataAnts.Columns("BestShare").Visible = chkAny.Checked

            Case "chkShowPools"
                Me.dataAnts.Columns("Pools").Visible = chkAny.Checked

            Case "chkShowFans"
                Me.dataAnts.Columns("Fans").Visible = chkAny.Checked

            Case "chkShowTemps"
                Me.dataAnts.Columns("Temps").Visible = chkAny.Checked

            Case "chkShowStatus"
                Me.dataAnts.Columns("Status").Visible = chkAny.Checked

            Case "chkShowFreqs"
                Me.dataAnts.Columns("Freq").Visible = chkAny.Checked

            Case "chkShowHighFan"
                Me.dataAnts.Columns("HFan").Visible = chkAny.Checked

            Case "chkShowHighTemp"
                Me.dataAnts.Columns("HTemp").Visible = chkAny.Checked

            Case "chkShowXCount"
                Me.dataAnts.Columns("XCount").Visible = chkAny.Checked

            Case "chkShowRej"
                Me.dataAnts.Columns("Rej%").Visible = chkAny.Checked

            Case "chkShowStale"
                Me.dataAnts.Columns("Stale%").Visible = chkAny.Checked

            Case Else
                MsgBox(chkAny.Name & " not found!", MsgBoxStyle.Critical Or MsgBoxStyle.OkOnly)

        End Select

        Me.dataAnts.Refresh()

    End Sub

    Private Sub optAddS1_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles optAddS1.CheckedChanged, optAddS2.CheckedChanged

        Dim opt As RadioButton

        opt = sender

        If opt.Checked = True Then
            If opt.Name = "optAddS1" Then
                optAddS2.Checked = False
            Else
                optAddS1.Checked = False
            End If
        End If

    End Sub

    Private Sub chkUseAPI_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles chkUseAPI.CheckedChanged

        If chkUseAPI.Checked = True Then
            Me.chkWBRebootIfXd.Visible = False
            Me.chkWBSavePassword.Visible = False
            Me.lblWBPassword.Visible = False
            Me.lblWBUserName.Visible = False
            Me.txtWBPassword.Visible = False
            Me.txtWBUserName.Visible = False
        Else
            Me.chkWBRebootIfXd.Visible = True
            Me.chkWBSavePassword.Visible = True
            Me.lblWBPassword.Visible = True
            Me.lblWBUserName.Visible = True
            Me.txtWBPassword.Visible = True
            Me.txtWBUserName.Visible = True
        End If

    End Sub
End Class
