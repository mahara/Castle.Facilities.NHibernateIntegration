#region License
// Copyright 2004-2024 Castle Project - https://www.castleproject.org/
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

namespace Castle.Facilities.NHibernateIntegration.Tests.Issues.Facilities152;

using Castle.Core.Resource;
using Castle.Facilities.NHibernateIntegration.SessionStores;
using Castle.Windsor;
using Castle.Windsor.Configuration.Interpreters;

using NUnit.Framework;

[TestFixture]
public class Fixture
{
    [Test]
    public void ShouldReadIsWebConfigurationFromXmlRegistration()
    {
        var file1 = "Castle.Facilities.NHibernateIntegration.Tests/Issues.Facilities152.facilityweb.xml";
        var file2 = "Castle.Facilities.NHibernateIntegration.Tests/Issues.Facilities152.facilitynonweb.xml";

        var containerWhenIsWebTrue = new WindsorContainer(new XmlInterpreter(new AssemblyResource(file1)));
        var containerWhenIsWebFalse = new WindsorContainer(new XmlInterpreter(new AssemblyResource(file2)));

        var sessionStoreWhenIsWebTrue = containerWhenIsWebTrue.Resolve<ISessionStore>();
        var sessionStoreWhenIsWebFalse = containerWhenIsWebFalse.Resolve<ISessionStore>();

        Assert.That(sessionStoreWhenIsWebTrue, Is.InstanceOf(typeof(WebSessionStore)));
        Assert.That(sessionStoreWhenIsWebFalse, Is.InstanceOf(typeof(AsyncLocalSessionStore)));
    }
}
