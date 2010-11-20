'==========================================================================
'
'  File:        DIR.vb
'  Location:    FileSystem <Visual Basic .Net>
'  Description: DIR文件流类
'  Version:     2009.08.11.
'  Copyright(C) F.R.C.
'
'==========================================================================
'TODO:新建DIR游戏无法完全读取

Imports System
Imports System.Math
Imports System.Drawing
Imports System.IO
Imports System.Collections.Generic
Imports Firefly
Imports Firefly.TextEncoding

''' <summary>DIR文件流类</summary>
''' <remarks>
''' 用于打开盟军1的DIR文件
''' </remarks>
Public Class DIR
    Inherits StreamEx
    Private Sub New(ByVal Path As String, ByVal FileMode As FileMode, ByVal Access As FileAccess, ByVal Share As FileShare)
        MyBase.New(Path, FileMode, Access, Share)
    End Sub
    Shared Function Open(ByVal Path As String) As DIR
        Dim pf As DIR = Nothing
#If CONFIG <> "Debug" Then
        Try
#End If
        pf = New DIR(Path, FileMode.Open, FileAccess.Read, FileShare.Read)
        pf.Position = 0
        pf.RootValue = New FileDB(pf)
        Return pf
#If CONFIG <> "Debug" Then
        Catch
            Try
                pf.Close()
            Catch
            End Try
            Throw
        End Try
#End If
    End Function

    Private RootValue As FileDB
    Public ReadOnly Property Root() As FileDB
        Get
            Return RootValue
        End Get
    End Property


    Public Sub Extract(ByVal File As FileDB, ByVal Directory As String, Optional ByVal Mask As String = "*.*")
        With File
            Dim Dir As String = Directory.Trim.TrimEnd("\"c)
            If Dir <> "" AndAlso Not IO.Directory.Exists(Dir) Then IO.Directory.CreateDirectory(Dir)
            Select Case .Type
                Case FileDB.FileType.File
                    If IsMatchFileMask(.Name, Mask) Then
                        Dim t As New StreamEx(GetPath(Dir, .Name), FileMode.Create)
                        BaseStream.Position = .Address
                        For n As Integer = 0 To .Length - 1
                            t.WriteByte(CByte(BaseStream.ReadByte))
                        Next
                        t.Close()
                    End If
                Case FileDB.FileType.Directory
                    Dim d As String = GetPath(Dir, .Name)
                    If d <> "" AndAlso Not IO.Directory.Exists(d) Then IO.Directory.CreateDirectory(d)
                    Dim FileSet As New Queue(Of FileDB)
                    BaseStream.Position = .Address
                    Dim f As New FileDB(Me)
                    While f.Type <> FileDB.FileType.DirectoryEnd
                        FileSet.Enqueue(f)
                        f = New FileDB(Me)
                    End While
                    For Each FileDB As FileDB In FileSet
                        Extract(FileDB, GetPath(Dir, .Name), Mask)
                    Next
                Case Else
            End Select
        End With
    End Sub

    Sub New(ByVal Source As String, ByVal Path As String)
        MyBase.New(Path, FileMode.Create)
        Dim FileQueue As New Queue(Of FileDB)
        Dim FileLengthAddressPointerQueue As New Queue(Of Integer)
        Dim FilePathQueue As New Queue(Of String)
        Dim FileLengthQueue As New Queue(Of Integer)
        Dim FileAddressQueue As New Queue(Of Integer)
        Dim DirQueue As New Queue(Of FileDB)
        Dim DirAddressPointerQueue As New Queue(Of Integer)
        Dim DirAddressQueue As New Queue(Of Integer)

        Dim RootName As String = GetFileName(Source)
        If RootName.Length > 36 Then Throw New InvalidDataException(Source)

        Me.SetLength(16777216)
        Dim cFileDB As FileDB = FileDB.CreateDirectory(RootName, FileDB.DBLength * 2)
        RootValue = cFileDB
        cFileDB.WriteToFile(Me)
        FileDB.CreateDirectoryEnd().WriteToFile(Me)
        ImportDirectory(GetFileDirectory(Source), cFileDB, FileQueue, FileLengthAddressPointerQueue, FilePathQueue, DirQueue, DirAddressPointerQueue, DirAddressQueue)

        Dim File As StreamEx
        For Each f As String In FilePathQueue
            File = New StreamEx(f, FileMode.Open)
            FileLengthQueue.Enqueue(CInt(File.Length))
            FileAddressQueue.Enqueue(CInt(Me.Position))
            If Me.Length - Me.Position < File.Length Then
                Me.SetLength(CLng(Me.Length + Max(16777216, Ceiling(File.Length / 16777216) * 16777216)))
            End If
            For n As Integer = 0 To CInt(File.Length - 1)
                WriteByte(File.ReadByte)
            Next
            File.Close()
        Next
        Me.SetLength(Me.Position)

        Dim fn As FileDB
        Dim pl As Integer
        Dim pa As Integer
        For Each p As Integer In FileLengthAddressPointerQueue
            Me.Position = p
            fn = FileQueue.Dequeue
            pl = FileLengthQueue.Dequeue
            pa = FileAddressQueue.Dequeue
            fn.Length = pl
            fn.Address = pa
            WriteInt32(pl)
            WriteInt32(pa)
        Next
        Dim dn As FileDB
        Dim pda As Integer
        For Each p As Integer In DirAddressPointerQueue
            Me.Position = p
            dn = DirQueue.Dequeue
            pda = DirAddressQueue.Dequeue
            dn.Address = pda
            WriteInt32(pda)
        Next
        Me.Position = 0
    End Sub
    Private Sub ImportDirectory(ByVal Dir As String, ByVal DirDB As FileDB, ByVal FileQueue As Queue(Of FileDB), ByVal FileLengthAddressPointerQueue As Queue(Of Integer), ByVal FilePathQueue As Queue(Of String), ByVal DirQueue As Queue(Of FileDB), ByVal DirAddressPointerQueue As Queue(Of Integer), ByVal DirAddressQueue As Queue(Of Integer))
        Dim cFileDB As FileDB
        Dim Name As String
        DirDB.SubFileDB = New List(Of FileDB)
        For Each f As String In Directory.GetFiles(GetPath(Dir, DirDB.Name))
            Name = GetFileName(f)
            If Name.Length > 36 Then Throw New InvalidDataException(f)
            cFileDB = FileDB.CreateFile(Name, -1, -1)
            cFileDB.WriteToFile(Me)
            cFileDB.ParentFileDB = DirDB
            DirDB.SubFileDB.Add(cFileDB)
            FileQueue.Enqueue(cFileDB)
            FileLengthAddressPointerQueue.Enqueue(CInt(Me.Position - 8))
            FilePathQueue.Enqueue(f)
        Next
        Dim DirListInner As New Queue(Of FileDB)
        For Each d As String In Directory.GetDirectories(GetPath(Dir, DirDB.Name))
            Name = GetFileName(d)
            If Name.Length > 36 Then Throw New InvalidDataException(d)
            cFileDB = FileDB.CreateDirectory(Name, -1)
            cFileDB.WriteToFile(Me)
            cFileDB.ParentFileDB = DirDB
            DirDB.SubFileDB.Add(cFileDB)
            DirQueue.Enqueue(cFileDB)
            DirListInner.Enqueue(cFileDB)
            DirAddressPointerQueue.Enqueue(CInt(Me.Position - 4))
        Next
        FileDB.CreateDirectoryEnd().WriteToFile(Me)

        For Each d As String In Directory.GetDirectories(GetPath(Dir, DirDB.Name))
            cFileDB = DirListInner.Dequeue
            DirAddressQueue.Enqueue(CInt(Me.Position))
            ImportDirectory(GetFileDirectory(d), cFileDB, FileQueue, FileLengthAddressPointerQueue, FilePathQueue, DirQueue, DirAddressPointerQueue, DirAddressQueue)
        Next
    End Sub

    'End Of Class
    'Start Of SubClasses

    Public Class FileDB
        Public Name As String
        Public Type As FileType
        Public Length As Int32
        Public Address As Int32
        Public Const DBLength As Integer = 44
        Public ParentFileDB As FileDB
        Public SubFileDB As List(Of FileDB)
        Private Sub New(ByVal Name As String, ByVal Type As FileType, ByVal Length As Int32, ByVal Address As Int32)
            If Name <> "" Then Me.Name = Name.ToUpper
            Me.Type = Type
            Me.Length = Length
            Me.Address = Address
        End Sub
        Public Sub New(ByVal s As DIR)
            Name = s.ReadString(32, ASCII)
            Type = CType(s.ReadByte, FileType)
            s.Position += 3
            Length = s.ReadInt32
            Address = s.ReadInt32
            If Type = FileType.Directory Then
                Dim TempAddress As Integer = CInt(s.Position)
                SubFileDB = New List(Of FileDB)
                s.Position = Address
                Dim f As New FileDB(s)
                While f.Type <> FileType.DirectoryEnd
                    f.ParentFileDB = Me
                    SubFileDB.Add(f)
                    f = New FileDB(s)
                End While
                s.Position = TempAddress
            End If
        End Sub
        Public Enum FileType As Byte
            File = 0
            Directory = 1
            DirectoryEnd = 255
        End Enum
        Public Shared Function CreateFile(ByVal Name As String, ByVal Length As Int32, ByVal Address As Int32) As FileDB
            Return New FileDB(Name, FileType.File, Length, Address)
        End Function
        Public Shared Function CreateDirectory(ByVal Name As String, ByVal Address As Int32) As FileDB
            Return New FileDB(Name, FileType.Directory, 0, Address)
        End Function
        Public Shared Function CreateDirectoryEnd() As FileDB
            Return New FileDB("DIRECTOR.FIN", FileType.DirectoryEnd, &HFFFFFFFF, &HFFFFFFFF)
        End Function
        Public Sub WriteToFile(ByVal s As DIR)
            s.WriteString(Name, 32, ASCII)
            s.WriteInt32(Type Or &HCDCDCD00)
            s.WriteInt32(Length)
            s.WriteInt32(Address)
        End Sub
    End Class
End Class
