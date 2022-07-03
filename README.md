# Castle.Facilities.NHibernateIntegration

Castle.Facilities.NHibernateIntegration

## License

Castle.Facilities.NHibernateIntegration is &copy; 2004-2022 Castle Project. It is free software, and may be redistributed under the terms of the [Apache 2.0](http://opensource.org/licenses/Apache-2.0) license.

## NuGet Preview Feed

If you would like to use preview NuGet's from our CI builds on AppVeyor, you can add the following NuGet source to your project:

```
https://ci.appveyor.com/nuget/windsor-qkry8n2r6yak
```

## Tests

The test cases run against a MSSQL Server 2008 database by default.
But due to NHibernate's features, you can change the database to whathever you like.

1. Modify the connection properties information.

   See file "facilityconfig.xml" for full details.

2. Create two databases (test and test2 on a MSSQL Server will do).



