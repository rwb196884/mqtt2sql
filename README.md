# mqtt2sql

SQL Server, in particular. Shouldn't be difficult to support other databases.

This listens to MQTT topics ad whenever it sees a message it logs the message, topic, and timestamp to a table in a database.

## Setup and run

```
> cd mqtt2sql
> dotnet restore
> dotnet build
```

Now edit the `appsettings.json`.

```
> dotnet tool install --global dotnet-ef
> dotnet ef database update
> dotnet run
```

I run this on my Debian 10 box that is on 24/7 and also does home routing, file sharing, DLNA, SQL Server, mqtt, etc. like this:
```
# cd mqtt2sql
# /usr/bin/screen -dm -S bf -L -Logfile /foot/mqtt/mqtt-screen.log dotnet run --project /root/mqtt/Mqtt2Sql.csproj
```

The table is created by an EF migration from the model class `` and looks like this:
```
CREATE TABLE Messages (
	Topic     nvarchar(450) NOT NULL,
	Timestamp datetime2(7)  NOT NULL,
	Payload   nvarchar(max)     NULL,
	
	CONSTRAINT PK_Messages PRIMARY KEY CLUSTERED (Topic, Timestamp)
)
```

Besides actually creating the database, you will also need to:
```
CREATE LOGIN Mqtt WITH Password = 'MqttPassword', CHECK_POLICY = OFF
USE Mqtt
CREATE USER Mqtt FOR LOGIN Mqtt
EXEC sp_addrolemember 'db_owner', 'Mqtt'
```


## Ideas

To get messages from `mqtt` it would probably be easier to do something like 
```
mosquitto_sub -h localhost -p 1883 -t zigbee2mqtt/Temp1/# -F "%I %t %p"
```
but I don't know how to go about using the output from that.