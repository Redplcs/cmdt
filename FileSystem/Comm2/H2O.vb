'==========================================================================
'
'  File:        H2O.vb
'  Location:    FileSystem <Visual Basic .Net>
'  Description: MBI文件类
'  Version:     2009.08.11.
'  Copyright(C) F.R.C.
'
'==========================================================================
'UNDONE

Option Strict On
Imports System
Imports System.Math
Imports System.Collections.Generic
Imports System.Drawing
Imports System.IO
Imports Microsoft.VisualBasic
Imports Firefly
Imports Firefly.Imaging

''' <summary>H2O文件类</summary>
Public Class H2O
    Public AlphaTable As Int32(,)
    Public b As Bmp

    Public Sub New(ByVal Path As String)
        Dim s As StreamEx = Nothing
        With Me
#If CONFIG <> "Debug" Then
            Try
#End If
            s = New StreamEx(Path, FileMode.Open, FileAccess.Read, FileShare.Read)

            Dim BackColor As Color = Color.FromArgb(58, 81, 90)
            Dim ForeColor As Color = Color.Black

            s.Position = 18
            .AlphaTable = New Int32(255, 255) {}
            For y As Integer = 0 To 255
                For x As Integer = 0 To 255
                    Dim Alpha As Integer = s.ReadByte
                    Dim r As Integer = (BackColor.R * (255 - Alpha) + ForeColor.R * Alpha) \ 255
                    Dim g As Integer = (BackColor.G * (255 - Alpha) + ForeColor.G * Alpha) \ 255
                    Dim b As Integer = (BackColor.B * (255 - Alpha) + ForeColor.B * Alpha) \ 255
                    .AlphaTable(x, y) = Color.FromArgb(r, g, b).ToArgb
                Next
            Next

            .b = New Bmp(256, 256, 32)
            .b.SetRectangle(0, 0, .AlphaTable)

            s.Close()
#If CONFIG <> "Debug" Then
            Catch
                Try
                    s.Close()
                Catch
                End Try
                Throw
            End Try
#End If
        End With
    End Sub

    Public Sub ExportToGif(ByVal Index As Integer, ByVal Path As String)
        'Dim i As MBIPicture = Picture(Index)
        'Dim g As New GifImageBlock(i.Rectangle, i.Palette)

        'Dim gf As New Gif(g)
        'gf.WriteToFile(Path)
    End Sub
End Class
