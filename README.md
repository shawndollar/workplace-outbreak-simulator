## Workplace Outbreak Simulator
This repository contains applications that simulate a viral outbreak in a workplace.

There is one solution called WorkplaceOutbreakSimulator. It contains the following three projects:
* WorkplaceOutbreakSimulatorEngine - .NET Standard 2.0 class library responsible for all of the simulation and output.
* WorkplaceOutbreakSimulatorConsole - .NET Core 3.1 console app that will use the default simulator configuration and will write all contact events out to a CSV file. Output folder should be specified as first command line arg.
* WorkplaceOutbreakSimulatorWebApp - .NET Core 3.1 ASP.NET Web with Razor Pages app that allows you to run the simulation for various configurations and download the contact event logs for employees one at a time (for faster simulations and smaller logs).

#### The simulator uses the following parameters:

* given a building with 5 floors 
* floor 1 - 30 people ,2 rooms
* floor 2 - 150 people, 12 office rooms, 1 breakroom, 2 meeting rooms
* floor 3- 150 people, 15 office rooms, 1 breakroom, 2 meeting rooms
* floor 4- 150 people, 5 office rooms, 1 breakroom, 3 meeting rooms
* floor 5- 50 people 50 office rooms, 1 breakroom, 2 meeting rooms
* 8 Hour workday
* simulate each hour of workday for 4 months
* random distribution in rooms.
* 25% of people use breakrooms once a day from noon till 1pm
* random 4-8 people will meet in random meeting rooms for one hour each hour except during noon to 1pm
* each person will randomly be a man or woman
* each person will have a random first and last name
* each person will have a unique identifier
* each person will belong in a room on a floor
* starting with one sick person
* infection rate 20% ~~((random between 2.8 and 7.4))~~
* different stages of the sickness
* well -> infected(3 days) --> incubation(3-7 days) -->symptomatic(random number of days between 6-11 days) --> immune
* can transmit during infected, incubation and symptomatic stages
* if sick 35% chance to get tested. takes 3 days to get results back
* if results positive then that person would go home/hospital for 5 days and then return to work
* there will be a log that tracks each person movements hourly for each workday, which stage they are in and people they have come into contact with during that hour
* this log will need to be able to be exported to .csv

#### Various Assumptions

1. Anyone in any viral stage is "sick".
2. 5 day work week.
3. Everyone uses breakroom on his or her own floor first (else random).
4. Assume anyone can have a meeting on on any floor.
5. Infection rate stays the same whether around 1 infected person or 10 infected people.
6. Employees *cannot* be infected in an office.
7. Only times at the office are logged, but all times (including weekends) count towards each viral stage length.

#### External API(s)

The employee data is retrieved from https://mockaroo.com. You can quickly create a free account and get an API Key and update the app settings files, if necessary. The free account allows for 200 requests every 24 hours.

#### Performance

A full 4 month simulation can take about 2 minutes, and the full output file can be large - approximately 115 MB.
