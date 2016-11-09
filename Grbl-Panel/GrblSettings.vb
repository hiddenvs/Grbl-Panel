﻿Imports System.Windows.Forms
Imports System.Threading.Thread

Partial Class GrblGui
    Public Class GrblSettings
        ' Handle settings related operations


        Private _gui As GrblGui
        Private _paramTable As DataTable    ' to hold the parameters
        Private _nextParam As Integer       ' to track which param line is next

        Private _settings As Dictionary(Of String, String) = New Dictionary(Of String, String) From {
            {"0", "step pulse, usec"},
            {"1", "step idle delay, msec"},
            {"2", "step port invert mask:"},
            {"3", "dir port invert mask:"},
            {"4", "step enable invert, bool"},
            {"5", "limit pins invert, bool"},
            {"6", "probe pin invert, bool"},
            {"10", "status report mask"},
            {"11", "junction deviation, mm"},
            {"12", "arc tolerance, mm"},
            {"13", "report inches, bool"},
            {"20", "soft limits, bool"},
            {"21", "hard limits, bool"},
            {"22", "homing cycle, bool"},
            {"23", "homing dir invert mask"},
            {"24", "homing feed, mm/min"},
            {"25", "homing seek, mm/min"},
            {"26", "homing debounce, msec"},
            {"27", "homing pull-off, mm"},
            {"30", "rpm max"},
            {"31", "rpm min"},
            {"100", "x, step/mm"},
            {"101", "y, step/mm"},
            {"102", "z, step/mm"},
            {"103", "a, step/mm"},
            {"110", "x max rate, mm/min"},
            {"111", "y max rate, mm/min"},
            {"112", "z max rate, mm/min"},
            {"113", "a max rate, mm/min"},
            {"120", "x accel, mm/sec^2"},
            {"121", "y accel, mm/sec^2"},
            {"122", "z accel, mm/sec^2"},
            {"123", "a accel, mm/sec^2"},
            {"130", "x max travel, mm"},
            {"131", "y max travel, mm"},
            {"132", "z max travel, mm"},
            {"133", "a max travel, mm"}
            }
#Region "Properties"
        ReadOnly Property IsHomingEnabled As Integer
            Get
                Dim row As DataRow
                row = _paramTable.Rows.Find("$22")
                If Not IsNothing(row) Then
                    Return row.Item("Value")
                End If
                Return 0
            End Get
        End Property
        ''' <summary>
        ''' Gets a value indicating whether Grbl is override capable.
        ''' </summary>
        ''' <value>
        ''' <c>true</c> if Grbl is override capable; otherwise, <c>false</c>.
        ''' </value>
        ReadOnly Property IsOverrideCapable As Boolean
            Get
                Dim row As DataRow
                row = _paramTable.Rows.Find("$31")
                If Not IsNothing(row) Then
                    Return True
                End If
                Return False
            End Get
        End Property

#End Region

        Public Sub New(ByRef gui As GrblGui)
            ' Get ref to parent object
            _gui = gui
            ' For Connected events
            AddHandler(_gui.Connected), AddressOf GrblConnected

        End Sub
        Public Sub EnableState(ByVal yes As Boolean)

            If yes Then
                _gui.gbGrblSettings.Enabled = True
            Else
                _gui.gbGrblSettings.Enabled = False
            End If
        End Sub

        Private Sub GrblConnected(ByVal msg As String)     ' Handles GrblGui.Connected Event
            If msg = "Connected" Then

                ' We are connected to Grbl so populate the Settings
                _nextParam = 0
                gcode.sendGCodeLine("$$")
            End If
        End Sub

        Public Sub FillSettings(ByVal data As String)
            ' Add a settings line to the display
            'Console.WriteLine("GrblSettings: $ Data is : " + data)
            ' Return
            Dim params() As String
            Dim index As Integer
            If _nextParam = 0 Then
                ' We are dealing with a fresh
                _paramTable = New DataTable
                With _paramTable
                    .Columns.Add("ID")
                    .Columns.Add("Value")
                    .Columns.Add("Description")
                    .PrimaryKey = New DataColumn() { .Columns("ID")}
                End With
            End If
            params = data.Split({"="c, "("c, ")"c})
            params(1) = params(1).Replace(" ", "")         ' strip trailing blanks
            If (params.Count = 4) Then
                _paramTable.Rows.Add(params(0), params(1), params(2))
            Else
                ' We have Grbl in GUI mode, so add Description manually
                index = params(0).Remove(0, 1)      ' remove the leading $
                If _settings.ContainsKey(index) Then
                    _paramTable.Rows.Add(params(0), params(1), _settings(index))
                Else
                    _paramTable.Rows.Add(params(0), params(1), "")
                End If
            End If
            _nextParam += 1
            If params(0) = _gui.tbSettingsGrblLastParam.Text Then ' We got the last one
                _nextParam = 0            ' in case user does a MDI $$
                With _gui.dgGrblSettings
                    .DataSource = _paramTable
                    .Columns("ID").Width = 40
                    .Columns("ID").ReadOnly = True
                    .Columns("ID").DefaultCellStyle.BackColor = SystemColors.Control
                    .Columns("Value").Width = 60
                    .Columns("Description").Width = 200
                    .Columns("Description").ReadOnly = True
                    .Columns("Description").DefaultCellStyle.BackColor = SystemColors.Control
                    .Refresh()
                End With
                ' Tell everyone we have the params
                RaiseEvent GrblSettingsRetrieved()
            End If
        End Sub

        ' Event template for Settings Retrieved indication
        Public Event GrblSettingsRetrieved()

        Public Sub RefreshSettings()
            _nextParam = 0
            gcode.sendGCodeLine("$$")
        End Sub
    End Class

    ' We need a way to propogate changes on the Settings tab, do that here

    Private Sub btnSettingsRefreshMisc_Click(sender As Object, e As EventArgs) Handles btnSettingsRefreshMisc.Click, btnSettingsRefreshPosition.Click, btnSettingsRefreshJogging.Click
        Dim b As Button = sender
        Select Case DirectCast(b.Tag, String)
            Case "Misc"
                changeStatusRate(My.Settings.StatusPollInterval)
                prgBarQ.Maximum = My.Settings.QBuffMaxSize
                prgbRxBuf.Maximum = My.Settings.RBuffMaxSize
                cbStatusPollEnable.Checked = My.Settings.StatusPollEnabled
                cbSettingsConnectOnLoad.Checked = My.Settings.GrblConnectOnLoad

            Case "Position"
                tbSettingsSpclPosition1.Text = My.Settings.MachineSpclPosition1
                tbSettingsSpclPosition2.Text = My.Settings.MachineSpclPosition2


            Case "Jogging"
                tbSettingsFIImperial.Text = My.Settings.JoggingFIImperial
                tbSettingsFRImperial.Text = My.Settings.JoggingFRImperial
                tbSettingsFIMetric.Text = My.Settings.JoggingFIMEtric
                tbSettingsFRMetric.Text = My.Settings.JoggingFRMetric
                cbSettingsMetric.Checked = My.Settings.JoggingUnitsMetric

                setDistanceMetric(cbSettingsMetric.Checked)

                btnXPlus.Interval = 1000 / My.Settings.JoggingXRepeat
                btnXMinus.Interval = 1000 / My.Settings.JoggingXRepeat
                btnYPlus.Interval = 1000 / My.Settings.JoggingYRepeat
                btnYMinus.Interval = 1000 / My.Settings.JoggingYRepeat
                btnZPlus.Interval = 1000 / My.Settings.JoggingZRepeat
                btnZMinus.Interval = 1000 / My.Settings.JoggingZRepeat
        End Select
    End Sub


    Private Sub btnSettingsGrbl_Click(sender As Object, e As EventArgs) Handles btnSettingsGrbl.Click
        ' Retrieve settings from Grbl
        settings.RefreshSettings()
    End Sub

    Private Sub dgGrblSettings_CellMouseDoubleClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles dgGrblSettings.CellMouseDoubleClick
        ' User wants to edit a Grbl setting
        ' We do these one at a time due to EEProm write restrictions
        'Psuedo code:
        ' Which row? Get new value, determine $id, send to Grbl, do a refresh ($$)
        If e.ColumnIndex <> 1 Then
            ' ignore the click, it is not in Value column
            Return
        End If
        Dim gridView As DataGridView = sender
        If Not IsNothing(gridView.EditingControl) Then
            ' we have something to change (aka ignore errant double clicks)
            Dim param As String = gridView.Rows(e.RowIndex).Cells(0).Value.ToString & "=" & gridView.EditingControl.Text
            gcode.sendGCodeLine(param)
            Sleep(200)              ' Have to wait for EEProm write
            gcode.sendGCodeLine("$$")   ' Refresh
        End If
    End Sub
End Class
