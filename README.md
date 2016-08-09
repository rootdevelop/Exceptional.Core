# Exceptional.Core
Lite implementation of MS SQL Exception logger for ASP.Net Core (based on StackExchange.Exceptional)   

To implement add the following in Startup.cs:   

```
Exceptional.Core.ErrorStore.Setup(new SQLErrorStore("your connection string here"));
 
services.AddMvc(config =>
            {
                config.Filters.Add(new GlobalExceptionFilter());
            });
```

Please note this is highly experimental and lacking a lot of features. Just a quick hack to get basic exception logging working!
