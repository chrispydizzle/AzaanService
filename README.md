# AzaanService 2.0
a basic C# worker intended for use as a linux daemon. The program: 
1. grabs times from a webservice to use as a schedule once a day 
2. at scheduled times, finds a configurable cast device of choice
3. streams a configurable soundfile to the cast device 
<br /><br />Utilizes the API at 
-     http://muslimsalat.com/api/#auto

Leverages the very cool [GoogleCast](https://github.com/kakone/GoogleCast) Nuget package. 

Future ideas for this:
- Cast To All
- Alexa support
 
<br />v1.0 python - less features at https://github.com/chrispydizzle/azaanhome