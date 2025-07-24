This workflow describes how to create a new driver from scratch.

The template repo is found at:
https://github.com/biosero/gbgdriver-project-templates.git

This machine has the template installed already.
To create a new project using the template run the following command in the terminal:
dotnet new GBG_DT -n {ProjectName} -I {InstrumentName} -M {ManufacturerName}

For the Brooks IntelliXCap 96 the information looks as so:
{ProjectName} = Brooks.IntelliXCap.Driver
{InstrumentName} = IntelliXCap96
{ManufacturerName} = Brooks

The first step to create a new driver is to check 