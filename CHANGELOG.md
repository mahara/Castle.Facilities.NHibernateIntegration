# Castle.Facilities.NHibernateIntegration - Changelog

## 5.3.x (202x-xx-xx)

Breaking Changes:
- Upgraded to .NET 6.0 and .NET Framework 4.8.
- Upgraded Castle.Services.Transaction version to 5.3.0
- Upgraded Castle.Facilities.AutoTx version to 5.3.0
- Replaced Castle.Services.Transaction.TransactionMode with System.Transactions.TransactionScopeOption.
- Replaced Castle.Services.Transaction.IsolationMode with System.Transactions.IsolationLevel.


## 5.2.0 (2022-06-25)

Improvements:
- Added AsyncLocalSessionStore.
- Updated Castle.Windsor version to 5.1.2
- Updated Castle.Services.Transaction version to 5.2.0
- Updated Castle.Facilities.AutoTx version to 5.2.0

Breaking Changes:
- Set AsyncLocalSessionStore as the default SessionStore.


## 5.1.0 (2022-02-20)

Improvements:
- Updated Castle.Core version to 4.4.1
- Updated Castle.Windsor version to 5.1.1

Breaking Changes:
- Upgraded NHibernate version to 5.3.10


## 5.0.0 (2021-05-31)

Improvements:
- Upgraded to SDK-style projects.

Breaking Changes:
- Removed .NET Framework 3.5, .NET Framework 4.0, and .NET Framework 4.0 Client Profile supports
- Upgraded Castle.Core version to 4.4.0
- Upgraded Castle.Windsor version to 5.0.0
- Upgraded NHibernate version to 5.2.7


## 4.0.0 (2018-09-18)

Improvements:
- Allow the facility to work with async methods and TPL/multithreading ([#1](https://github.com/mahara/Castle.Facilities.NHibernateIntegration/issues/1))

Breaking Changes:
- Upgraded NHibernate version to 4.0.0 GA and 3.3.4 GA


## 3.3.3 (2014-08-18)

Breaking Changes:
- Upgraded Castle.Core version to 3.3.0
- Upgraded Castle.Windsor version to 3.3.0
- Upgraded Castle.Services.Transaction version to 3.3.0
- Upgraded Castle.Facilities.AutoTx version to 3.3.0



