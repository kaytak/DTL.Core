﻿'    Raoult Property Package 
'    Copyright 2012 Daniel Wagner O. de Medeiros
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


Namespace DTL.SimulationObjects.PropertyPackages.Auxiliary

    <Serializable()> Public Class Ideal

        Function H_RA_MIX(ByVal TYPE As String, ByVal T As Double, ByVal P As Double, ByVal Vz As Object, ByVal VKij As Object, ByVal VTc As Object, ByVal VPc As Object, ByVal Vw As Object, ByVal VMM As Object, ByVal Hid As Double, ByVal HVap As Array) As Double

            Dim i, n As Integer

            n = Vz.Length - 1

            Dim R, DHres As Double

            R = 8.314

            i = 0
            Dim MMm = 0
            Do
                MMm += Vz(i) * VMM(i)
                i += 1
            Loop Until i = n + 1

            If TYPE = "L" Then
                Dim val As Double
                For i = 0 To n
                    If T / VTc(i) <= 1 Then val += Vz(i) * HVap(i) * VMM(i)
                Next
                DHres = -val
            Else
                DHres = 0
            End If

            H_RA_MIX = Hid + DHres / MMm

        End Function

        Function S_RA_MIX(ByVal TYPE As String, ByVal T As Double, ByVal P As Double, ByVal Vz As Array, ByVal VKij As Object, ByVal VTc As Array, ByVal VPc As Array, ByVal Vw As Array, ByVal VMM As Array, ByVal Sid As Double, ByVal HVap As Array, ByVal Hid As Double) As Double

            If TYPE = "L" Then
                Return Sid + Me.H_RA_MIX(TYPE, T, P, Vz, VKij, VTc, VPc, Vw, VMM, Hid, HVap) / T
            Else
                Return Sid + Me.H_RA_MIX(TYPE, T, P, Vz, VKij, VTc, VPc, Vw, VMM, Hid, HVap) / T
            End If

        End Function

        Function CpCv(ByVal TYPE, ByVal T, ByVal P, ByVal Vz, ByVal VKij, ByVal Vzmass, ByVal VTc, ByVal VPc, ByVal VCpig, ByVal VMM, ByVal Vw, ByVal VZRa)

            Dim n As Double
            Dim i As Integer

            n = Vz.Length - 1

            i = 0
            Dim MMm = 0
            Do
                MMm += Vz(i) * VMM(i)
                i += 1
            Loop Until i = n + 1

            Dim Cpm_ig = 0
            i = 0
            Do
                Cpm_ig += Vzmass(i) * VCpig(i) * MMm
                i += 1
            Loop Until i = n + 1

            Dim Cv = Cpm_ig - 8.314
            Dim Cp = Cpm_ig

            Dim Cp_Cv2 = Cp / Cv

            Dim tmp(2) As Double
            tmp(0) = Cp_Cv2
            tmp(1) = Cp / MMm
            tmp(2) = Cv / MMm

            CpCv = tmp

        End Function

    End Class

End Namespace