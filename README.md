# Castle.Facilities.NHibernateIntegration

Castle.Facilities.NHibernateIntegration

## License

Castle.Facilities.NHibernateIntegration is &copy; 2004-2019 Castle Project. It is free software, and may be redistributed under the terms of the [Apache 2.0](http://opensource.org/licenses/Apache-2.0) license.

## Running the Tests

The tests run against a MSSQL Server 2008 by default.
But due to NHibernate's features, you can change the database server to any of your choice.

1. Create two databases on the database server, e.g.: "test" and "test2".

2. Modify the database connection properties in "Castle.Facilities.NHibernateIntegration.Tests" project to use the databases created.
   See "App.config" and all "**\facility.xml" files for full details.
