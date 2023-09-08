# -*- coding: utf-8 -*-
"""
@author: marrob
230811 1033

A szekvencia idításához használd ezt szkriptet.
A szkript nélkül a TestStand nem állítja be az Environment-et, ami a korábbi ECUTSBMW-vel nem kompatiblis.

Ha új szekvenciát készítesz, akkor a script fájlnevét nevezd át:
pl ha az új szekvenciád neve: UjTest.seq
script neve: UjTest__Start.py


A TTC500-as szekvencia betöléskor ellenörzi az Environemnet-et egy Stations Golobal változó segítségével
StationGlobals.Constants.Environment == "TTC"
"""

import os
from pathlib import Path


# *** Example ****
#  Source TTC500__Start.py
#           then
# Call Sequence is:TTC500.seq  
#

try:
    scirptFileName = Path(__file__).name
    fileName = scirptFileName.split('__')[0]
    
    
    #*** Test Stand ****
    # SeqEdit.exe /env "MyEnvironment.tsenv"
    #
    
    testStandPath = r"C:\Program Files (x86)\National Instruments\TestStand 2019\Bin\SeqEdit.exe"
    sequencePath = fr"{os.getcwd()}\{fileName}.seq"
    environmentPath = fr"{os.getcwd()}\Ethernet_Env.tsenv"
    os.startfile(testStandPath, arguments = f'"{sequencePath}" /env "{environmentPath}"')
    
    print(f"Working directory:{os.getcwd()}")
    print(f"Starting Sequence:'{sequencePath}'")
    print(f'TestStand argument:{sequencePath} /env "{environmentPath}"')
except Exception as e:
    print(f"Hiba:{e}")
    
    
#input()