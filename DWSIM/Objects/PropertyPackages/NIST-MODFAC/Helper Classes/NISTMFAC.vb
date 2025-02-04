﻿'    Modified UNIFAC (NIST) Property Package 
'    Copyright 2015 Daniel Wagner O. de Medeiros
'    Copyright 2015 Gregor Reichert
'
'    Based on the paper entitled "New modified UNIFAC parameters using critically 
'    evaluated phase equilibrium data", http://dx.doi.org/10.1016/j.fluid.2014.12.042
'
'    This file is part of DWSIM.
'
'    DWSIM is free software: you can redistribute it and/or modify
'    it under the terms of the GNU General Public License as published by
'    the Free Software Foundation, either version 3 of the License, or
'    (at your option) any later version.
'
'    DWSIM is distributed in the hope that it will be useful,
'    but WITHOUT ANY WARRANTY; without even the implied warranty of
'    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
'    GNU General Public License for more details.
'
'    You should have received a copy of the GNU General Public License
'    along with DWSIM.  If not, see <http://www.gnu.org/licenses/>.

Imports Microsoft.VisualBasic.FileIO

Namespace DTL.SimulationObjects.PropertyPackages.Auxiliary

    <Serializable()> Public Class NISTMFAC

        Public Shadows ModfGroups As NistModfacGroups

        Sub New()

            ModfGroups = New NistModfacGroups

        End Sub


        Function ID2Group(ByVal id As Integer) As String

            For Each group As ModfacGroup In Me.ModfGroups.Groups.Values
                If group.Secondary_Group = id Then Return group.GroupName
            Next

            Return ""

        End Function

        Function Group2ID(ByVal groupname As String) As String

            For Each group As ModfacGroup In Me.ModfGroups.Groups.Values
                If group.GroupName = groupname Then
                    Return group.Secondary_Group
                End If
            Next

            Return 0

        End Function

        Function GAMMA_MR(ByVal T As Double, ByVal Vx As Double(), ByVal VQ As Double(), ByVal VR As Double(), ByVal VEKI As List(Of Dictionary(Of Integer, Double))) As Double()

            CheckParameters(VEKI)

            Dim i, m, k As Integer

            Dim n = Vx.Length - 1

            Dim Vgammac(n), Vgammar(n), Vgamma(n) As Double
            Dim Q(n), R(n), j(n), L(n), val As Double
            Dim j_(n)

            Dim teta, s As New Dictionary(Of Integer, Double)
            Dim beta As New List(Of Dictionary(Of Integer, Double))

            i = 0
            For Each item In VEKI
                beta.Add(New Dictionary(Of Integer, Double))
                For Each item2 In VEKI
                    For Each item3 In item2
                        If Not beta(i).ContainsKey(item3.Key) Then beta(i).Add(item3.Key, 0.0#)
                    Next
                Next
                i += 1
            Next

            Dim ids As Integer() = beta(0).Keys.ToArray

            Dim n2 As Integer = ids.Length - 1

            For i = 0 To n
                For m = 0 To n2
                    For k = 0 To n2
                        If VEKI(i).ContainsKey(ids(k)) Then beta(i)(ids(m)) += VEKI(i)(ids(k)) * TAU(ids(k), ids(m), T)
                    Next
                Next
            Next

            i = 0
            For Each item In VEKI
                For Each item2 In item
                    For Each item3 In item
                    Next
                Next
                i += 1
            Next

            Dim sum_xq = 0.0#
            i = 0
            Do
                Q(i) = VQ(i)
                sum_xq = sum_xq + Vx(i) * Q(i)
                i = i + 1
            Loop Until i = n + 1

            i = 0
            For Each item In VEKI
                For Each item2 In item
                    val = Vx(i) * Q(i) * VEKI(i)(item2.Key) / sum_xq
                    If Not teta.ContainsKey(item2.Key) Then teta.Add(item2.Key, val) Else teta(item2.Key) += val
                Next
                i += 1
            Next

            For Each item In teta
                For Each item2 In teta
                    val = teta(item2.Key) * TAU(item2.Key, item.Key, T)
                    If Not s.ContainsKey(item.Key) Then s.Add(item.Key, val) Else s(item.Key) += val
                Next
            Next

            Dim sum_xr = 0.0#
            Dim sum_xr_ = 0.0#
            i = 0
            Do
                R(i) = VR(i)
                sum_xr = sum_xr + Vx(i) * R(i)
                sum_xr_ = sum_xr_ + Vx(i) * R(i) ^ (3 / 4)
                i = i + 1
            Loop Until i = n + 1

            i = 0
            Do
                j(i) = R(i) / sum_xr
                j_(i) = R(i) ^ (3 / 4) / sum_xr_
                L(i) = Q(i) / sum_xq
                Vgammac(i) = 1 - j_(i) + Math.Log(j_(i)) - 5 * Q(i) * (1 - j(i) / L(i) + Math.Log(j(i) / L(i)))
                k = 0
                Dim tmpsum = 0.0#
                For Each item2 In teta
                    If VEKI(i).ContainsKey(item2.Key) Then
                        tmpsum += item2.Value * beta(i)(item2.Key) / s(item2.Key) - VEKI(i)(item2.Key) * Math.Log(beta(i)(item2.Key) / s(item2.Key))
                    Else
                        tmpsum += item2.Value * beta(i)(item2.Key) / s(item2.Key)
                    End If
                Next
                Vgammar(i) = Q(i) * (1 - tmpsum)
                Vgamma(i) = Math.Exp(Vgammac(i) + Vgammar(i))
                If Vgamma(i) = 0 Then Vgamma(i) = 0.000001
                i = i + 1
            Loop Until i = n + 1

            Return Vgamma

        End Function

        Sub CheckParameters(ByVal VEKI As List(Of Dictionary(Of Integer, Double)))

            Dim ids As New ArrayList

            For Each item In VEKI
                For Each item2 In item
                    If item(item2.Key) <> 0.0# And Not ids.Contains(item2.Key) Then ids.Add(item2.Key)
                Next
            Next

            For Each id1 As Integer In ids
                For Each id2 As Integer In ids
                    If id1 <> id2 Then
                        Dim g1, g2 As Integer
                        g1 = Me.ModfGroups.Groups(id1).PrimaryGroup
                        g2 = Me.ModfGroups.Groups(id2).PrimaryGroup
                        If Me.ModfGroups.InteracParam_aij.ContainsKey(g1) Then
                            If Not Me.ModfGroups.InteracParam_aij(g1).ContainsKey(g2) Then
                                If Me.ModfGroups.InteracParam_aij.ContainsKey(g2) Then
                                    If Not Me.ModfGroups.InteracParam_aij(g2).ContainsKey(g1) And g2 <> g1 Then
                                        Throw New Exception("NIST-MODFAC Error: Could not find interaction parameter for groups " & Me.ModfGroups.Groups(id1 + 1).GroupName & " / " & _
                                                            Me.ModfGroups.Groups(id2 + 1).GroupName & ". Activity coefficient calculation will give you inconsistent results for this system.")
                                    End If
                                End If
                            End If
                        Else
                            If Me.ModfGroups.InteracParam_aij.ContainsKey(g2) Then
                                If Not Me.ModfGroups.InteracParam_aij(g2).ContainsKey(g1) And g2 <> g1 Then
                                    Throw New Exception("NIST-MODFAC Error: Could not find interaction parameter for groups " & Me.ModfGroups.Groups(id1 + 1).GroupName & " / " & _
                                                        Me.ModfGroups.Groups(id2 + 1).GroupName & ". Activity coefficient calculation will give you inconsistent results for this system.")
                                End If
                            Else
                                Throw New Exception("NIST-MODFAC Error: Could not find interaction parameter for groups " & Me.ModfGroups.Groups(id1 + 1).GroupName & " / " & _
                                                    Me.ModfGroups.Groups(id2 + 1).GroupName & ". Activity coefficient calculation will give you inconsistent results for this system.")
                            End If
                        End If
                    End If
                Next
            Next

        End Sub

        Function TAU(ByVal group_1, ByVal group_2, ByVal T)

            Dim g1, g2 As Integer
            Dim res As Double

            g1 = Me.ModfGroups.Groups(group_1).PrimaryGroup
            g2 = Me.ModfGroups.Groups(group_2).PrimaryGroup

            If g1 <> g2 Then
                If Me.ModfGroups.InteracParam_aij.ContainsKey(g1) Then
                    If Me.ModfGroups.InteracParam_aij(g1).ContainsKey(g2) Then
                        res = Me.ModfGroups.InteracParam_aij(g1)(g2) + Me.ModfGroups.InteracParam_bij(g1)(g2) * T + Me.ModfGroups.InteracParam_cij(g1)(g2) * T ^ 2
                    Else
                        If Me.ModfGroups.InteracParam_aij.ContainsKey(g2) Then
                            If Me.ModfGroups.InteracParam_aij(g2).ContainsKey(g1) Then
                                res = Me.ModfGroups.InteracParam_aij(g2)(g1) + Me.ModfGroups.InteracParam_bij(g2)(g1) * T + Me.ModfGroups.InteracParam_cij(g2)(g1) * T ^ 2
                            Else
                                res = 0.0#
                            End If
                        Else
                            res = 0.0#
                        End If
                    End If
                ElseIf Me.ModfGroups.InteracParam_aij.ContainsKey(g2) Then
                    If Me.ModfGroups.InteracParam_aij(g2).ContainsKey(g1) Then
                        res = Me.ModfGroups.InteracParam_aij(g2)(g1) + Me.ModfGroups.InteracParam_bij(g2)(g1) * T + Me.ModfGroups.InteracParam_cij(g2)(g1) * T ^ 2
                    Else
                        res = 0.0#
                    End If
                Else
                    res = 0.0#
                End If
            Else
                res = 0.0#
            End If

            Return Math.Exp(-res / T)

        End Function

        Function RET_Ri(ByVal VN As Dictionary(Of Integer, Double)) As Double

            Dim i As Integer = 0
            Dim res As Double

            For Each kvp As KeyValuePair(Of Integer, Double) In VN
                res += Me.ModfGroups.Groups(kvp.Key).R * VN(kvp.Key)
                i += 1
            Next

            Return res

        End Function

        Function RET_Qi(ByVal VN As Dictionary(Of Integer, Double)) As Double

            Dim i As Integer = 0
            Dim res As Double

            For Each kvp As KeyValuePair(Of Integer, Double) In VN
                res += Me.ModfGroups.Groups(kvp.Key).Q * VN(kvp.Key)
                i += 1
            Next

            Return res

        End Function

        Function RET_EKI(ByVal VN As Dictionary(Of Integer, Double), ByVal Q As Double) As Dictionary(Of Integer, Double)

            Dim i As Integer = 0
            Dim res As New Dictionary(Of Integer, Double)

            For Each kvp As KeyValuePair(Of Integer, Double) In VN
                res.Add(kvp.Key, Me.ModfGroups.Groups(kvp.Key).Q * VN(kvp.Key) / Q)
                i += 1
            Next

            Return res

        End Function

        Function RET_VN(ByVal cp As BaseThermoClasses.ConstantProperties) As Dictionary(Of Integer, Double)

            Dim i As Integer = 0
            Dim res As New Dictionary(Of Integer, Double)
            Dim added As Boolean = False

            res.Clear()

            For Each group As ModfacGroup In Me.ModfGroups.Groups.Values
                If cp.NISTMODFACGroups.Collection.Count > 0 Then
                    For Each s As String In cp.NISTMODFACGroups.Collection.Keys
                        If s = group.Secondary_Group Then
                            res.Add(group.Secondary_Group, cp.NISTMODFACGroups.Collection(s))
                            Exit For
                        End If
                    Next
                Else
                    For Each s As String In cp.MODFACGroups.Collection.Keys
                        If s = group.Secondary_Group Then
                            res.Add(group.Secondary_Group, cp.MODFACGroups.Collection(s))
                            Exit For
                        End If
                    Next
                End If
            Next

            Return res

        End Function

        Function DLNGAMMA_DT(ByVal T As Double, ByVal Vx As Array, ByVal VQ As Double(), ByVal VR As Double(), ByVal VEKI As List(Of Dictionary(Of Integer, Double))) As Array

            Dim gamma1, gamma2 As Double()

            Dim epsilon As Double = 0.001

            gamma1 = GAMMA_MR(T, Vx, VQ, VR, VEKI)
            gamma2 = GAMMA_MR(T + epsilon, Vx, VQ, VR, VEKI)

            Dim dgamma(gamma1.Length - 1) As Double

            For i As Integer = 0 To Vx.Length - 1
                dgamma(i) = (gamma2(i) - gamma1(i)) / (epsilon)
            Next

            Return dgamma

        End Function

        Function HEX_MIX(ByVal T As Double, ByVal Vx As Array, ByVal VQ As Double(), ByVal VR As Double(), ByVal VEKI As List(Of Dictionary(Of Integer, Double))) As Double

            Dim dgamma As Double() = DLNGAMMA_DT(T, Vx, VQ, VR, VEKI)

            Dim hex As Double = 0.0#

            For i As Integer = 0 To Vx.Length - 1
                hex += -8.314 * T ^ 2 * Vx(i) * dgamma(i)
            Next

            Return hex 'kJ/kmol

        End Function

        Function CPEX_MIX(ByVal T As Double, ByVal Vx As Array, ByVal VQ As Double(), ByVal VR As Double(), ByVal VEKI As List(Of Dictionary(Of Integer, Double))) As Double

            Dim hex1, hex2, cpex As Double

            Dim epsilon As Double = 0.001

            hex1 = HEX_MIX(T, Vx, VQ, VR, VEKI)
            hex2 = HEX_MIX(T + epsilon, Vx, VQ, VR, VEKI)

            cpex = (hex2 - hex1) / epsilon

            Return cpex 'kJ/kmol.K

        End Function

    End Class

    <Serializable()> Public Class NistModfacGroups

        Public InteracParam_aij As Dictionary(Of Integer, Dictionary(Of Integer, Double))
        Public InteracParam_bij As Dictionary(Of Integer, Dictionary(Of Integer, Double))
        Public InteracParam_cij As Dictionary(Of Integer, Dictionary(Of Integer, Double))

        Protected m_groups As Dictionary(Of Integer, ModfacGroup)

        Sub New()

            Dim pathsep = IO.Path.DirectorySeparatorChar

            m_groups = New Dictionary(Of Integer, ModfacGroup)
            InteracParam_aij = New Dictionary(Of Integer, Dictionary(Of Integer, Double))
            InteracParam_bij = New Dictionary(Of Integer, Dictionary(Of Integer, Double))
            InteracParam_cij = New Dictionary(Of Integer, Dictionary(Of Integer, Double))

            Dim cult As Globalization.CultureInfo = New Globalization.CultureInfo("en-US")
            Dim fields As String()
            Dim delimiter As String = vbTab
            Dim maingroup As Integer = 1
            Dim mainname As String = ""

            Using stream As IO.Stream = New IO.MemoryStream(My.Resources.NIST_MODFAC_RiQi)
                Using reader As New IO.StreamReader(stream)
                    Using parser As New TextFieldParser(reader)
                        parser.SetDelimiters(delimiter)
                        parser.ReadLine()
                        parser.ReadLine()
                        Dim i As Integer = 1
                        parser.SetDelimiters(delimiter)
                        While Not parser.EndOfData
                            fields = parser.ReadFields()
                            If fields(0).StartsWith("(") Then
                                maingroup = fields(0).Split(")")(0).Substring(1)
                                mainname = fields(0).Trim().Split(")")(1).Trim
                            Else
                                Me.Groups.Add(fields(0), New ModfacGroup(fields(1), mainname, maingroup, fields(0), Double.Parse(fields(2), cult), Double.Parse(fields(3), cult)))
                                i += 1
                            End If
                        End While
                    End Using
                End Using
            End Using

            Using stream As IO.Stream = New IO.MemoryStream(My.Resources.NIST_MODFAC_IP)
                Using reader As New IO.StreamReader(stream)
                    Using parser As New TextFieldParser(reader)
                        parser.SetDelimiters(delimiter)
                        fields = parser.ReadFields()
                        While Not parser.EndOfData
                            fields = parser.ReadFields()
                            If Not Me.InteracParam_aij.ContainsKey(fields(0)) Then
                                Me.InteracParam_aij.Add(fields(0), New Dictionary(Of Integer, Double))
                                Me.InteracParam_aij(fields(0)).Add(fields(1), Double.Parse(fields(2), cult))
                                Me.InteracParam_bij.Add(fields(0), New Dictionary(Of Integer, Double))
                                Me.InteracParam_bij(fields(0)).Add(fields(1), Double.Parse(fields(3), cult))
                                Me.InteracParam_cij.Add(fields(0), New Dictionary(Of Integer, Double))
                                Me.InteracParam_cij(fields(0)).Add(fields(1), Double.Parse(fields(4), cult) / 1000)
                            Else
                                If Not Me.InteracParam_aij(fields(0)).ContainsKey(fields(1)) Then
                                    Me.InteracParam_aij(fields(0)).Add(fields(1), Double.Parse(fields(2), cult))
                                    Me.InteracParam_bij(fields(0)).Add(fields(1), Double.Parse(fields(3), cult))
                                    Me.InteracParam_cij(fields(0)).Add(fields(1), Double.Parse(fields(4), cult) / 1000)
                                Else
                                    Me.InteracParam_aij(fields(0))(fields(1)) = Double.Parse(fields(2), cult)
                                    Me.InteracParam_bij(fields(0))(fields(1)) = Double.Parse(fields(3), cult)
                                    Me.InteracParam_cij(fields(0))(fields(1)) = Double.Parse(fields(4), cult) / 1000
                                End If
                            End If
                        End While
                    End Using
                End Using
            End Using

        End Sub

        Public ReadOnly Property Groups() As Dictionary(Of Integer, ModfacGroup)
            Get
                Return m_groups
            End Get
        End Property

    End Class

End Namespace
