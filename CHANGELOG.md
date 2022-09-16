# Castle.Facilities.NHibernateIntegration - Changelog


## 5.4.0 (2022-09-xx)


## 5.3.0 (2022-09-17)

Breaking Changes:
- Upgraded to **`.NET 6.0`** and **`.NET Framework 4.8`**.
- Updated **`Castle.Services.Transaction`** version to 5.3.0.
- Updated **`Castle.Facilities.AutoTx`** version to 5.3.0.
- Replaced ```Castle.Services.Transaction.IsolationMode``` with ```System.Transactions.IsolationLevel```.
- Renamed ```IsolationMode``` to ```IsolationLevel```.


## 5.2.0 (2022-06-25)

Improvements:
- Updated **`Castle.Windsor`** version to 5.1.2.
- Updated **`NHibernate`** version to 5.3.12.

Breaking Changes:
- Updated **`Castle.Services.Transaction`** version to 5.2.0.
- Updated **`Castle.Facilities.AutoTx`** version to 5.2.0.


## 5.1.0 (2022-02-20)

Improvements:
- Updated **`Castle.Core`** version to 4.4.1.
- Updated **`Castle.Windsor`** version to 5.1.1.

Breaking Changes:
- Updated **`Castle.Services.Transaction`** version to 5.1.0.
- Updated **`Castle.Facilities.AutoTx`** version to 5.1.0.
- Updated **`NHibernate`** version to 5.3.10.


## 5.0.0 (2021-05-31)

Improvements:
- Upgraded to SDK-style .NET projects
  (https://learn.microsoft.com/en-us/dotnet/core/project-sdk/overview).
- Added **.NET (Core)** support.
- Upgraded to **.NET Framework 4.7.2**.
- Added ```AsyncLocalSessionStore``` and ```ThreadLocalSessionStore```.

Breaking Changes:
- Removed **.NET Framework 3.5**, **.NET Framework 4.0**, and **.NET Framework 4.0 Client Profile** supports.
- Removed **Mono** support.
- Updated **`Castle.Core`** version to 4.4.0.
- Updated **`Castle.Windsor`** version to 5.0.0.
- Updated **`Castle.Services.Transaction`** version to 5.0.0.
- Updated **`Castle.Facilities.AutoTx`** version to 5.0.0.
- Updated **`NHibernate`** version to 5.2.7.
- Change default ```SessionStore``` in ```NHibernateFacility``` from ```LogicalCallContextSessionStore``` to ```AsyncLocalSessionStore``` .
- Refactored ```AbstractDictionaryStackSessionStore```.


## 4.0.0 (2018-09-18)

Improvements:
- Allowed facility to work with async methods and TPL/multithreading ([#1](https://github.com/mahara/Castle.Facilities.NHibernateIntegration/issues/1)).

Breaking Changes:
- Updated **`NHibernate`** versions to 4.0.0 GA and 3.3.4 GA.


## 3.3.3 (2014-08-18)

Breaking Changes:
- Updated **`Castle.Core`** version to 3.3.0.
- Updated **`Castle.Windsor`** version to 3.3.0.
- Updated **`Castle.Services.Transaction`** version to 3.3.0.
- Updated **`Castle.Facilities.AutoTx`** version to 3.3.0.
