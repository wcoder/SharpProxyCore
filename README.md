# SharpProxyCore

SharpProxy is a simple cross-platform proxy server developed with the intent of being able to open up local web development servers.

This allows you to test, hit breakpoints, and generally do development by using other machines and mobile devices.

Simply enter the local port number of your development server and map it with an external port to host on.

Based on [SharpProxy](https://github.com/jocull/SharpProxy).

## Use

```sh
dotnet run -p src/SharpProxyCore.csproj
```

### Options

- `i=|internal` - Set internal port [Default 5000]
- `e=|external` - Set external port [Default 8888]

### Example

1. Start you development server (for example on `http://127.0.0.1:8887`)

2. Start SharpProxyCore with options

```sh
dotnet run -p src/SharpProxyCore.csproj --internal=8887 --external=5001
```

Proxy output:

```
Started:
http://192.168.43.46:5001 => http://127.0.0.1:8887
```

3. Create request from any client

```csharp
var client = new HttpClient();
var data = await client.GetStringAsync("http://192.168.43.46:5001");
```

---
&copy; 2018 | MIT