﻿//-----------------------------------------------------------------------
// <copyright file="ProjectCollection_Tests.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>Tests for ProjectCollection</summary>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Shared;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using InvalidProjectFileException = Microsoft.Build.Exceptions.InvalidProjectFileException;

namespace Microsoft.Build.UnitTests.OM.Definition
{
    /// <summary>
    /// Tests for ProjectCollection
    /// </summary>
    [TestClass]
    public class ProjectCollection_Tests
    {
        /// <summary>
        /// Gets or sets the test context.
        /// </summary>
        public TestContext TestContext { get; set; }

        /// <summary>
        /// Clear out the global project collection
        /// </summary>
        [TestInitialize]
        public void SetUp()
        {
            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
        }

        /// <summary>
        /// Clear out the global project collection
        /// </summary>
        [TestCleanup]
        public void TearDown()
        {
            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
            Assert.AreEqual(0, ProjectCollection.GlobalProjectCollection.Count);

            IDictionary<string, string> globalProperties = ProjectCollection.GlobalProjectCollection.GlobalProperties;
            foreach (string propertyName in globalProperties.Keys)
            {
                ProjectCollection.GlobalProjectCollection.RemoveGlobalProperty(propertyName);
            }

            Assert.AreEqual(0, ProjectCollection.GlobalProjectCollection.GlobalProperties.Count);
        }

        /// <summary>
        /// Add a single project from disk and verify it's put in the global project collection
        /// </summary>
        [TestMethod]
        public void AddProjectFromDisk()
        {
            string path = null;

            try
            {
                path = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
                ProjectRootElement xml = ProjectRootElement.Create(path);
                xml.Save();

                Project project = new Project(path);

                Project project2 = ProjectCollection.GlobalProjectCollection.LoadProject(path);
                Assert.AreEqual(true, Object.ReferenceEquals(project, project2));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// When an unnamed project is saved, it gets a name, and should be entered into 
        /// the appropriate project collection.
        /// </summary>
        [TestMethod]
        public void AddProjectOnSave()
        {
            string path = null;

            try
            {
                Project project = new Project();
                Assert.AreEqual(0, ProjectCollection.GlobalProjectCollection.Count);

                path = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
                project.Save(path);

                Project project2 = ProjectCollection.GlobalProjectCollection.LoadProject(path);
                Assert.AreEqual(true, Object.ReferenceEquals(project, project2));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// When an unnamed project is saved, it gets a name, and should be entered into 
        /// the appropriate project collection.
        /// </summary>
        [TestMethod]
        public void AddProjectOnSave_SpecifiedProjectCollection()
        {
            string path = null;

            try
            {
                ProjectCollection collection = new ProjectCollection();
                Project project = new Project(collection);

                path = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
                project.Save(path);

                Project project2 = collection.LoadProject(path);
                Assert.AreEqual(true, Object.ReferenceEquals(project, project2));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// When an unnamed project is given a name, it should be entered into its
        /// project collection.
        /// </summary>
        [TestMethod]
        public void AddProjectOnSetName()
        {
            Project project = new Project();
            project.FullPath = "c:\\x";

            Project project2 = ProjectCollection.GlobalProjectCollection.LoadProject("c:\\x");
            Assert.AreEqual(true, Object.ReferenceEquals(project, project2));
        }

        /// <summary>
        /// Loading a project from a file inherits the project collection's global properties
        /// </summary>
        [TestMethod]
        public void GlobalPropertyInheritLoadFromFile()
        {
            string path = null;

            try
            {
                path = CreateProjectFile();

                ProjectCollection collection = new ProjectCollection();
                collection.SetGlobalProperty("p", "v");
                Project project = collection.LoadProject(path);

                Assert.AreEqual("v", project.GlobalProperties["p"]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Loading a project from a file inherits the project collection's global properties
        /// </summary>
        [TestMethod]
        public void GlobalPropertyInheritLoadFromFile2()
        {
            string path = null;

            try
            {
                path = CreateProjectFile();

                ProjectCollection collection = new ProjectCollection();
                collection.SetGlobalProperty("p", "v");
                Project project = collection.LoadProject(path, "4.0");

                Assert.AreEqual("v", project.GlobalProperties["p"]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Loading a project from a file inherits the project collection's global properties
        /// </summary>
        [TestMethod]
        public void GlobalPropertyInheritLoadFromFile3()
        {
            string path = null;

            try
            {
                path = CreateProjectFile();

                ProjectCollection collection = new ProjectCollection();
                collection.SetGlobalProperty("p", "v");
                Project project = collection.LoadProject(path, null, "4.0");

                Assert.AreEqual("v", project.GlobalProperties["p"]);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Loading a project from a reader inherits the project collection's global properties
        /// </summary>
        [TestMethod]
        public void GlobalPropertyInheritLoadFromXml1()
        {
            XmlReader reader = CreateProjectXmlReader();

            ProjectCollection collection = new ProjectCollection();
            collection.SetGlobalProperty("p", "v");

            Project project = collection.LoadProject(reader);

            Assert.AreEqual("v", project.GlobalProperties["p"]);
        }

        /// <summary>
        /// Loading a project from a reader inherits the project collection's global properties
        /// </summary>
        [TestMethod]
        public void GlobalPropertyInheritLoadFromXml2()
        {
            XmlReader reader = CreateProjectXmlReader();

            ProjectCollection collection = new ProjectCollection();
            collection.SetGlobalProperty("p", "v");

            Project project = collection.LoadProject(reader, "4.0");

            Assert.AreEqual("v", project.GlobalProperties["p"]);
        }

        /// <summary>
        /// Creating a project inherits the project collection's global properties
        /// </summary>
        [TestMethod]
        public void GlobalPropertyInheritProjectConstructor()
        {
            ProjectCollection collection = new ProjectCollection();
            collection.SetGlobalProperty("p", "v");

            Project project = new Project(collection);

            Assert.AreEqual("v", project.GlobalProperties["p"]);
        }

        /// <summary>
        /// Load project should load a project, if it wasn't already loaded.
        /// </summary>
        [TestMethod]
        public void GetLoadedProjectNonExistent()
        {
            string path = null;

            try
            {
                path = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
                ProjectRootElement xml = ProjectRootElement.Create();
                xml.Save(path);
                Assert.AreEqual(0, ProjectCollection.GlobalProjectCollection.Count);

                Project result = ProjectCollection.GlobalProjectCollection.LoadProject(path);

                Assert.AreEqual(1, ProjectCollection.GlobalProjectCollection.Count);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Verify that one project collection doesn't contain the projects of another
        /// </summary>
        [TestMethod]
        public void GetLoadedProjectWrongCollection()
        {
            Project project1 = new Project();
            project1.FullPath = "c:\\1";

            ProjectCollection collection = new ProjectCollection();
            Project project2 = new Project(collection);
            project2.FullPath = "c:\\1";

            Assert.AreEqual(true, Object.ReferenceEquals(project2, collection.LoadProject("c:\\1")));
            Assert.AreEqual(false, Object.ReferenceEquals(project1, collection.LoadProject("c:\\1")));
        }

        /// <summary>
        /// Verify that one project collection doesn't contain the ProjectRootElements of another
        /// -- because they don't share a ProjectRootElementCache
        /// </summary>
        [TestMethod]
        public void GetLoadedProjectRootElementWrongCollection()
        {
            string path = null;

            try
            {
                path = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
                ProjectRootElement.Create(path).Save();

                ProjectCollection collection1 = new ProjectCollection();
                Project project1 = collection1.LoadProject(path);
                Project project1b = collection1.LoadProject(path);

                Assert.AreEqual(true, Object.ReferenceEquals(project1.Xml, project1b.Xml));

                ProjectCollection collection2 = new ProjectCollection();
                Project project2 = collection2.LoadProject(path);

                Assert.AreEqual(false, Object.ReferenceEquals(project1.Xml, project2.Xml));
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Attempt to have two equivalent projects in a project collection fails.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ErrorTwoProjectsEquivalentOneCollection()
        {
            Project project = new Project();
            project.FullPath = "c:\\x";

            Project project2 = new Project();
            project2.FullPath = "c:\\x";
        }

        /// <summary>
        /// Validates that when loading two projects with nominally different global properties, but that match when we take 
        /// into account the ProjectCollection's global properties, we get the pre-existing project if one exists. 
        /// </summary>
        [TestMethod]
        public void TwoProjectsEquivalentWhenOneInheritsFromProjectCollection()
        {
            Project project = new Project();
            project.FullPath = "c:\\1";

            // Set a global property on the project collection -- this should be passed on to all 
            // loaded projects. 
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Configuration", "Debug");

            Assert.AreEqual("Debug", project.GlobalProperties["Configuration"]);

            // now create a global properties dictionary to pass to a new project 
            Dictionary<string, string> globals = new Dictionary<string, string>();

            globals.Add("Configuration", "Debug");
            Project project2 = ProjectCollection.GlobalProjectCollection.LoadProject("c:\\1", globals, null);

            Assert.AreEqual(1, ProjectCollection.GlobalProjectCollection.Count);
        }

        /// <summary>
        /// Two projects may have the same path but different global properties.
        /// </summary>
        [TestMethod]
        public void TwoProjectsDistinguishedByGlobalPropertiesOnly()
        {
            ProjectRootElement xml = ProjectRootElement.Create();

            Dictionary<string, string> globalProperties1 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            globalProperties1.Add("p", "v1");
            Project project1 = new Project(xml, globalProperties1, "4.0");
            project1.FullPath = "c:\\1";

            Dictionary<string, string> globalProperties2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            globalProperties2.Add("p", "v2");
            Project project2 = new Project(xml, globalProperties2, "4.0");
            project2.FullPath = "c:\\1";

            Assert.AreEqual(true, Object.ReferenceEquals(project1, ProjectCollection.GlobalProjectCollection.LoadProject("c:\\1", globalProperties1, "4.0")));
            Assert.AreEqual(true, Object.ReferenceEquals(project2, ProjectCollection.GlobalProjectCollection.LoadProject("c:\\1", globalProperties2, "4.0")));

            List<Project> projects = Helpers.MakeList(ProjectCollection.GlobalProjectCollection.LoadedProjects);

            Assert.AreEqual(2, projects.Count);
            Assert.AreEqual(2, ProjectCollection.GlobalProjectCollection.Count);
            Assert.AreEqual(true, projects.Contains(project1));
            Assert.AreEqual(true, projects.Contains(project2));
        }

        /// <summary>
        /// Validates that we can correctly load two of the same project file with different global properties, even when
        /// those global properties are applied to the project by the project collection (and then overrided in one case). 
        /// </summary>
        [TestMethod]
        public void TwoProjectsDistinguishedByGlobalPropertiesOnly_ProjectOverridesProjectCollection()
        {
            Project project = new Project();
            project.FullPath = "c:\\1";

            // Set a global property on the project collection -- this should be passed on to all 
            // loaded projects. 
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Configuration", "Debug");

            Assert.AreEqual("Debug", project.GlobalProperties["Configuration"]);

            // Differentiate this project from the one below
            project.SetGlobalProperty("MyProperty", "MyValue");

            // now create a global properties dictionary to pass to a new project 
            Dictionary<string, string> project2Globals = new Dictionary<string, string>();

            project2Globals.Add("Configuration", "Release");
            project2Globals.Add("Platform", "Win32");
            Project project2 = ProjectCollection.GlobalProjectCollection.LoadProject("c:\\1", project2Globals, null);

            Assert.AreEqual("Release", project2.GlobalProperties["Configuration"]);

            // Setting a global property on the project collection overrides all contained projects, 
            // whether they were initially loaded with the global project collection's value or not. 
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Platform", "X64");
            Assert.AreEqual("X64", project.GlobalProperties["Platform"]);
            Assert.AreEqual("X64", project2.GlobalProperties["Platform"]);

            // But setting a global property on the project directly should override that.
            project2.SetGlobalProperty("Platform", "Itanium");
            Assert.AreEqual("Itanium", project2.GlobalProperties["Platform"]);

            // Now set global properties such that the two projects have an identical set.  
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Configuration", "Debug2");
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Platform", "X86");

            bool exceptionCaught = false;
            try
            {
                // This will make it identical, so we should get a throw here. 
                ProjectCollection.GlobalProjectCollection.SetGlobalProperty("MyProperty", "MyValue2");
            }
            catch (InvalidOperationException)
            {
                exceptionCaught = true;
            }

            Assert.IsTrue(exceptionCaught, "Should have caused the two projects to be identical, causing an exception to be thrown");
        }

        /// <summary>
        /// Two projects may have the same path but different tools version.
        /// </summary>
        [TestMethod]
        public void TwoProjectsDistinguishedByToolsVersionOnly()
        {
            if (ToolLocationHelper.GetPathToDotNetFramework(TargetDotNetFrameworkVersion.Version35) == null)
            {
                // "Requires 3.5 to be installed"
                return;
            }

            if (ToolLocationHelper.GetPathToDotNetFramework(TargetDotNetFrameworkVersion.Version20) == null)
            {
                // ".NET Framework 2.0 is required to be installed for this test, but it is not installed."
                return;
            }

            ProjectRootElement xml = ProjectRootElement.Create();

            Project project1 = new Project(xml, null, "2.0");
            project1.FullPath = "c:\\1";

            Project project2 = new Project(xml, null, "4.0");
            project2.FullPath = "c:\\1";

            Assert.AreEqual(true, Object.ReferenceEquals(project1, ProjectCollection.GlobalProjectCollection.LoadProject("c:\\1", null, "2.0")));
            Assert.AreEqual(true, Object.ReferenceEquals(project2, ProjectCollection.GlobalProjectCollection.LoadProject("c:\\1", null, "4.0")));
        }

        /// <summary>
        /// If the ToolsVersion in the project file is bogus, we'll default to the current ToolsVersion and successfully 
        /// load it.  Make sure we can RE-load it, too, and successfully pick up the correct copy of the loaded project. 
        /// </summary>
        [TestMethod]
        public void ReloadProjectWithInvalidToolsVersionInFile()
        {
            string content = @"
                    <Project ToolsVersion='bogus' xmlns='http://schemas.microsoft.com/developer/msbuild/2003' >
                        <Target Name='t'/>
                    </Project>
                ";

            Project project = new Project(XmlReader.Create(new StringReader(content)));
            project.FullPath = "c:\\123.proj";

            Project project2 = ProjectCollection.GlobalProjectCollection.LoadProject("c:\\123.proj", null, null);

            Assert.IsTrue(Object.ReferenceEquals(project, project2));
        }

        /// <summary>
        /// Make sure we can reload a project that has a ToolsVersion that doesn't match what it ends up getting 
        /// forced to by default (current). 
        /// </summary>
        [TestMethod]
        public void ReloadProjectWithProjectToolsVersionDifferentFromEffectiveToolsVersion()
        {
            string content = @"
                    <Project ToolsVersion='4.0' xmlns='http://schemas.microsoft.com/developer/msbuild/2003' >
                        <Target Name='t'/>
                    </Project>
                ";

            Project project = new Project(XmlReader.Create(new StringReader(content)));
            project.FullPath = "c:\\123.proj";

            Project project2 = ProjectCollection.GlobalProjectCollection.LoadProject("c:\\123.proj", null, null);

            Assert.IsTrue(Object.ReferenceEquals(project, project2));
        }

        /// <summary>
        /// Collection stores projects distinguished by path, global properties, and tools version.
        /// Changing global properties should update the collection.
        /// </summary>
        [TestMethod]
        public void ChangingGlobalPropertiesUpdatesCollection()
        {
            ProjectCollection collection = new ProjectCollection();
            Project project = new Project(collection);
            project.FullPath = "c:\\x"; // load into collection
            project.SetGlobalProperty("p", "v1"); // should update collection

            Dictionary<string, string> globalProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            globalProperties.Add("p", "v1");
            Project newProject = collection.LoadProject("c:\\x", globalProperties, null);

            Assert.AreEqual(true, Object.ReferenceEquals(project, newProject));
        }

        /// <summary>
        /// Changing global properties on collection should should update the collection's defaults,
        /// and any projects even if they have defined the same global properties
        /// </summary>
        [TestMethod]
        public void SettingGlobalPropertiesOnCollectionUpdatesProjects()
        {
            ProjectCollection collection = new ProjectCollection();
            Project project1 = new Project(collection);
            project1.FullPath = "c:\\y"; // load into collection
            Assert.AreEqual(0, project1.GlobalProperties.Count);

            collection.SetGlobalProperty("g1", "v1");
            collection.SetGlobalProperty("g2", "v2");
            collection.SetGlobalProperty("g2", "v2"); // try dupe

            Assert.AreEqual(2, project1.GlobalProperties.Count);

            collection.RemoveGlobalProperty("g2");
            Project project2 = new Project(collection);
            project2.FullPath = "c:\\x"; // load into collection

            Assert.AreEqual(1, project1.GlobalProperties.Count);
            Assert.AreEqual("v1", project2.GlobalProperties["g1"]);

            Assert.AreEqual(1, project2.GlobalProperties.Count);
            Assert.AreEqual("v1", project2.GlobalProperties["g1"]);
        }

        /// <summary>
        /// Changing global properties on collection should should update the collection's defaults,
        /// and any projects even if they have defined the same global properties
        /// </summary>
        [TestMethod]
        public void SettingGlobalPropertiesOnCollectionUpdatesProjects2()
        {
            ProjectCollection collection = new ProjectCollection();
            Project project1 = new Project(collection);
            project1.FullPath = "c:\\y"; // load into collection
            project1.SetGlobalProperty("g1", "v0");
            Helpers.ClearDirtyFlag(project1.Xml);

            collection.SetGlobalProperty("g1", "v1");
            collection.SetGlobalProperty("g2", "v2");

            Assert.AreEqual(2, project1.GlobalProperties.Count);
            Assert.AreEqual("v1", project1.GlobalProperties["g1"]);
            Assert.AreEqual("v2", project1.GlobalProperties["g2"]); // Got overwritten
            Assert.AreEqual(true, project1.IsDirty);
        }

        /// <summary>
        /// Changing global properties on collection should should update the collection's defaults,
        /// and all projects as well
        /// </summary>
        [TestMethod]
        public void RemovingGlobalPropertiesOnCollectionUpdatesProjects()
        {
            ProjectCollection collection = new ProjectCollection();
            Project project1 = new Project(collection);
            project1.FullPath = "c:\\y"; // load into collection
            Assert.AreEqual(0, project1.GlobalProperties.Count);

            Helpers.ClearDirtyFlag(project1.Xml);

            collection.SetGlobalProperty("g1", "v1"); // should make both dirty
            collection.SetGlobalProperty("g2", "v2"); // should make both dirty

            Assert.AreEqual(true, project1.IsDirty);

            Project project2 = new Project(collection);
            project2.FullPath = "c:\\x"; // load into collection

            Assert.AreEqual(true, project2.IsDirty);

            Assert.AreEqual(2, project1.GlobalProperties.Count);
            Assert.AreEqual("v1", project2.GlobalProperties["g1"]);

            Assert.AreEqual(2, project2.GlobalProperties.Count);
            Assert.AreEqual("v1", project2.GlobalProperties["g1"]);

            Helpers.ClearDirtyFlag(project1.Xml);
            Helpers.ClearDirtyFlag(project2.Xml);

            collection.RemoveGlobalProperty("g2"); // should make both dirty

            Assert.AreEqual(true, project1.IsDirty);
            Assert.AreEqual(true, project2.IsDirty);

            Assert.AreEqual(1, project1.GlobalProperties.Count);
            Assert.AreEqual(1, project2.GlobalProperties.Count);

            collection.RemoveGlobalProperty("g1");

            Assert.AreEqual(0, project1.GlobalProperties.Count);
            Assert.AreEqual(0, project2.GlobalProperties.Count);
        }

        /// <summary>
        /// Changing global properties on collection should should update the collection's defaults,
        /// and all projects as well
        /// </summary>
        [TestMethod]
        public void RemovingGlobalPropertiesOnCollectionUpdatesProjects2()
        {
            ProjectCollection collection = new ProjectCollection();
            collection.SetGlobalProperty("g1", "v1");

            Project project1 = new Project(collection);
            project1.FullPath = "c:\\y"; // load into collection
            project1.SetGlobalProperty("g1", "v0"); // mask collection property
            Helpers.ClearDirtyFlag(project1.Xml);

            collection.RemoveGlobalProperty("g1"); // should modify the project

            Assert.AreEqual(0, project1.GlobalProperties.Count);
            Assert.AreEqual(true, project1.IsDirty);
        }

        /// <summary>
        /// Unloading a project should remove it from the project collection
        /// </summary>
        [TestMethod]
        public void UnloadProject()
        {
            Project project = new Project();
            project.FullPath = "c:\\x"; // load into collection

            Assert.AreEqual(1, ProjectCollection.GlobalProjectCollection.Count);

            ProjectCollection.GlobalProjectCollection.UnloadProject(project); // should not throw

            Assert.AreEqual(0, ProjectCollection.GlobalProjectCollection.Count);
            Assert.AreEqual(0, Helpers.MakeList(ProjectCollection.GlobalProjectCollection.LoadedProjects).Count);
        }

        /// <summary>
        /// Unloading project XML should remove it from the weak cache.
        /// </summary>
        [TestMethod]
        public void UnloadProjectXml()
        {
            Project project = new Project();
            project.FullPath = "c:\\x"; // load into collection
            ProjectRootElement xml = project.Xml;

            // Unload the evaluation project, and then the XML.
            ProjectCollection.GlobalProjectCollection.UnloadProject(project);
            ProjectCollection.GlobalProjectCollection.UnloadProject(xml);

            try
            {
                // If the ProjectRootElement was unloaded from the cache, then
                // an attempt to load it by the pretend filename should fail,
                // so it makes a good test to see that the UnloadProject method worked.
                ProjectCollection.GlobalProjectCollection.LoadProject(xml.FullPath);
                Assert.Fail("An InvalidProjectFileException was expected.");
            }
            catch (InvalidProjectFileException)
            {
            }
        }

        /// <summary>
        /// Unloading project XML while it is in use should result in an exception.
        /// </summary>
        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void UnloadProjectXmlWhileInDirectUse()
        {
            Project project = new Project();
            project.FullPath = "c:\\x"; // load into collection

            // Attempt to unload the xml before unloading the project evaluation.
            ProjectCollection.GlobalProjectCollection.UnloadProject(project.Xml);
        }

        /// <summary>
        /// Unloading project XML while it is in use should result in an exception.
        /// </summary>
        [TestMethod, ExpectedException(typeof(InvalidOperationException))]
        public void UnloadProjectXmlWhileInImportUse()
        {
            Project mainProject = new Project();
            mainProject.FullPath = "c:\\main"; // load into collection

            Project importProject = new Project();
            importProject.FullPath = "c:\\import"; // load into collection
            ProjectRootElement importedXml = importProject.Xml;

            // Import into main project
            mainProject.Xml.PrependChild(mainProject.Xml.CreateImportElement(importProject.FullPath));
            mainProject.ReevaluateIfNecessary();

            // Unload the import evaluation, but not the main project that still has a reference to it.
            ProjectCollection.GlobalProjectCollection.UnloadProject(importProject);

            // Attempt to unload the import xml before unloading the project that still references it.
            try
            {
                ProjectCollection.GlobalProjectCollection.UnloadProject(importedXml);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Renaming a project should correctly update the project collection's set of loaded projects.
        /// </summary>
        [TestMethod]
        public void RenameProject()
        {
            Project project = new Project();
            project.FullPath = "c:\\x"; // load into collection

            project.FullPath = "c:\\y";

            Assert.AreEqual(1, ProjectCollection.GlobalProjectCollection.Count);

            Assert.AreEqual(true, Object.ReferenceEquals(project, Helpers.MakeList(ProjectCollection.GlobalProjectCollection.LoadedProjects)[0]));

            ProjectCollection.GlobalProjectCollection.UnloadProject(project); // should not throw

            Assert.AreEqual(0, ProjectCollection.GlobalProjectCollection.Count);
        }

        /// <summary>
        /// Validates that we don't somehow lose the ProjectCollection global properties when renaming the project. 
        /// </summary>
        [TestMethod]
        public void RenameProjectAndVerifyStillContainsProjectCollectionGlobalProperties()
        {
            Project project = new Project();
            project.FullPath = "c:\\1";

            // Set a global property on the project collection -- this should be passed on to all 
            // loaded projects. 
            ProjectCollection.GlobalProjectCollection.SetGlobalProperty("Configuration", "Debug");

            Assert.AreEqual("Debug", project.GlobalProperties["Configuration"]);

            project.FullPath = "c:\\2";

            Assert.AreEqual("Debug", project.GlobalProperties["Configuration"]);
        }

        /// <summary>
        /// Saving a project to a new name should correctly update the project collection's set of loaded projects.
        /// Reported by F#.
        /// </summary>
        [TestMethod]
        public void SaveToNewNameAndUnload()
        {
            string file1 = null;
            string file2 = null;

            try
            {
                file1 = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
                file2 = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();

                Project project = new Project();
                project.Save(file1);

                ProjectCollection collection = new ProjectCollection();

                Project project2 = collection.LoadProject(file1);
                project2.Save(file2);

                collection.UnloadProject(project2);
            }
            finally
            {
                File.Delete(file1);
                File.Delete(file2);
            }
        }

        /// <summary>
        /// Saving a project to a new name after loading, unloading, and reloading, should work.
        /// Reported by F#.
        /// </summary>
        [TestMethod]
        public void LoadUnloadReloadSaveToNewName()
        {
            string file1 = null;
            string file2 = null;

            try
            {
                file1 = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
                file2 = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();

                Project project = new Project();
                project.Save(file1);
                project.ProjectCollection.UnloadProject(project);

                ProjectCollection collection = new ProjectCollection();

                Project project2 = collection.LoadProject(file1);
                collection.UnloadProject(project2);

                Project project3 = collection.LoadProject(file1);
                project3.Save(file2); // should not crash

                collection.UnloadProject(project3);
            }
            finally
            {
                File.Delete(file1);
                File.Delete(file2);
            }
        }

        /// <summary>
        /// Saving a project to a new name after loading, unloading, and reloading, should work.
        /// Reported by F#.
        /// </summary>
        [TestMethod]
        public void LoadUnloadAllReloadSaveToNewName()
        {
            string file1 = null;
            string file2 = null;

            try
            {
                file1 = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
                file2 = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();

                Project project = new Project();
                project.Save(file1);
                project.ProjectCollection.UnloadProject(project);

                ProjectCollection collection = new ProjectCollection();

                Project project2 = collection.LoadProject(file1);
                collection.UnloadAllProjects();

                Project project3 = collection.LoadProject(file1);
                project3.Save(file2); // should not crash

                collection.UnloadProject(project3);
            }
            finally
            {
                File.Delete(file1);
                File.Delete(file2);
            }
        }

        /// <summary>
        /// Add a toolset
        /// </summary>
        [TestMethod]
        public void AddToolset()
        {
            ProjectCollection collection = new ProjectCollection();
            collection.RemoveAllToolsets();

            Toolset toolset = new Toolset("x", "c:\\y", collection, null);
            collection.AddToolset(toolset);

            Assert.AreEqual(toolset, collection.GetToolset("x"));
            Assert.AreEqual(true, collection.ContainsToolset("x"));

            List<Toolset> toolsets = Helpers.MakeList(collection.Toolsets);
            Assert.AreEqual(1, toolsets.Count);
            Assert.AreEqual(toolset, toolsets[0]);
        }

        /// <summary>
        /// Add two toolsets
        /// </summary>
        [TestMethod]
        public void AddTwoToolsets()
        {
            ProjectCollection collection = new ProjectCollection();
            collection.RemoveAllToolsets();

            Toolset toolset1 = new Toolset("x", "c:\\y", collection, null);
            Toolset toolset2 = new Toolset("y", "c:\\z", collection, null);

            collection.AddToolset(toolset1);
            collection.AddToolset(toolset2);

            Assert.AreEqual(toolset1, collection.GetToolset("x"));
            Assert.AreEqual(toolset2, collection.GetToolset("y"));

            List<Toolset> toolsets = Helpers.MakeList(collection.Toolsets);
            Assert.AreEqual(2, toolsets.Count);
            Assert.AreEqual(true, toolsets.Contains(toolset1));
            Assert.AreEqual(true, toolsets.Contains(toolset2));
        }

        /// <summary>
        /// Add a toolset that overrides another
        /// </summary>
        [TestMethod]
        public void ReplaceToolset()
        {
            ProjectCollection collection = new ProjectCollection();
            collection.RemoveAllToolsets();

            Toolset toolset1 = new Toolset("x", "c:\\y", collection, null);
            Toolset toolset2 = new Toolset("x", "c:\\z", collection, null);

            collection.AddToolset(toolset1);
            collection.AddToolset(toolset2);

            Assert.AreEqual(toolset2, collection.GetToolset("x"));

            List<Toolset> toolsets = Helpers.MakeList(collection.Toolsets);
            Assert.AreEqual(1, toolsets.Count);
            Assert.AreEqual(toolset2, toolsets[0]);
        }

        /// <summary>
        /// Attempt to add a null toolset
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddNullToolset()
        {
            ProjectCollection.GlobalProjectCollection.AddToolset(null);
        }

        /// <summary>
        /// Remove a toolset
        /// </summary>
        [TestMethod]
        public void RemoveToolset()
        {
            ProjectCollection collection = new ProjectCollection();

            Toolset toolset1 = new Toolset("x", "c:\\y", collection, null);
            Toolset toolset2 = new Toolset("y", "c:\\z", collection, null);

            int initial = Helpers.MakeList<Toolset>(collection.Toolsets).Count;

            collection.AddToolset(toolset1);
            collection.AddToolset(toolset2);

            Assert.AreEqual(true, collection.RemoveToolset("x"));
            Assert.AreEqual(false, collection.ContainsToolset("x"));

            Assert.AreEqual(1, Helpers.MakeList<Toolset>(collection.Toolsets).Count - initial);
        }

        /// <summary>
        /// Remove a nonexistent toolset
        /// </summary>
        [TestMethod]
        public void RemoveNonexistentToolset()
        {
            ProjectCollection collection = new ProjectCollection();
            Assert.AreEqual(false, collection.RemoveToolset("nonexistent"));
        }

        /// <summary>
        /// Attempt to remove a null tools version
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void RemoveNullToolsVersion()
        {
            ProjectCollection.GlobalProjectCollection.RemoveToolset(null);
        }

        /// <summary>
        /// Attempt to remove an empty string toolsversion
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void RemoveEmptyToolsVersion()
        {
            ProjectCollection.GlobalProjectCollection.RemoveToolset(String.Empty);
        }

        /// <summary>
        /// Current default from registry is 2.0 if 2.0 is installed
        /// </summary>
        [TestMethod]
        public void DefaultToolsVersion()
        {
            if (ToolLocationHelper.GetPathToDotNetFramework(TargetDotNetFrameworkVersion.Version20) == null)
            {
                // "Requires 2.0 to be installed"
                return;
            }

            ProjectCollection collection = new ProjectCollection();
            Assert.AreEqual("2.0", collection.DefaultToolsVersion);
        }

        /// <summary>
        /// Current default from registry is 4.0 if 2.0 is not installed
        /// </summary>
        [TestMethod]
        public void DefaultToolsVersion2()
        {
            if (ToolLocationHelper.GetPathToDotNetFramework(TargetDotNetFrameworkVersion.Version20) != null)
            {
                // "Requires 2.0 to NOT be installed"
                return;
            }

            ProjectCollection collection = new ProjectCollection();
            Assert.AreEqual(ObjectModelHelpers.MSBuildDefaultToolsVersion, collection.DefaultToolsVersion);
        }

        /// <summary>
        /// Error setting default tools version to empty
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SetDefaultToolsVersionEmpty()
        {
            ProjectCollection.GlobalProjectCollection.DefaultToolsVersion = String.Empty;
        }

        /// <summary>
        /// Error setting default tools version to a toolset that does not exist
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void SetDefaultToolsVersionNonexistentToolset()
        {
            ProjectCollection.GlobalProjectCollection.DefaultToolsVersion = "nonexistent";
        }

        /// <summary>
        /// Set default tools version; subsequent projects should use it 
        /// </summary>
        [TestMethod]
        public void SetDefaultToolsVersion()
        {
            ProjectCollection collection = new ProjectCollection();
            collection.AddToolset(new Toolset("x", @"c:\y", collection, null));

            collection.DefaultToolsVersion = "x";

            Assert.AreEqual("x", collection.DefaultToolsVersion);

            string content = ObjectModelHelpers.CleanupFileContents(@"
                    <Project xmlns='msbuildnamespace' >
                        <Target Name='t'/>
                    </Project>
                ");

            Project project = new Project(XmlReader.Create(new StringReader(content)), null, null, collection);

            // ... and after all that, we end up defaulting to the current ToolsVersion instead.  There's a way 
            // to turn this behavior (new in Dev12) off, but it requires setting an environment variable and 
            // clearing some internal state to make sure that the update environment variable is picked up, so 
            // there's not a good way of doing it from these deliberately public OM only tests. 
            Assert.AreEqual(project.ToolsVersion, ObjectModelHelpers.MSBuildDefaultToolsVersion);
        }

        /// <summary>
        /// Changes to the ProjectCollection object should raise a ProjectCollectionChanged event.
        /// </summary>
        [TestMethod]
        public void ProjectCollectionChangedEvent()
        {
            ProjectCollection collection = new ProjectCollection();
            bool dirtyRaised = false;
            ProjectCollectionChangedState expectedChange = ProjectCollectionChangedState.Loggers;
            collection.ProjectCollectionChanged +=
                (sender, e) =>
                {
                    Assert.AreSame(collection, sender);
                    Assert.AreEqual(expectedChange, e.Changed);
                    dirtyRaised = true;
                };
            Assert.IsFalse(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.DisableMarkDirty;
            dirtyRaised = false;
            collection.DisableMarkDirty = true; // LEAVE THIS TRUE for rest of the test, to verify it doesn't suppress these events
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.IsBuildEnabled;
            dirtyRaised = false;
            collection.IsBuildEnabled = !collection.IsBuildEnabled;
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.OnlyLogCriticalEvents;
            dirtyRaised = false;
            collection.OnlyLogCriticalEvents = !collection.OnlyLogCriticalEvents;
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.SkipEvaluation;
            dirtyRaised = false;
            collection.SkipEvaluation = !collection.SkipEvaluation;
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.GlobalProperties;
            dirtyRaised = false;
            collection.SetGlobalProperty("a", "b");
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.GlobalProperties;
            dirtyRaised = false;
            collection.RemoveGlobalProperty("a");
            Assert.IsTrue(dirtyRaised);

            // Verify HostServices changes raise the event.
            expectedChange = ProjectCollectionChangedState.HostServices;
            dirtyRaised = false;
            collection.HostServices = new Execution.HostServices();
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.Loggers;
            dirtyRaised = false;
            collection.RegisterLogger(new MockLogger());
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.Loggers;
            dirtyRaised = false;
            collection.RegisterLoggers(new Microsoft.Build.Framework.ILogger[] { new MockLogger(), new MockLogger() });
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.Loggers;
            dirtyRaised = false;
            collection.UnregisterAllLoggers();
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.Toolsets;
            dirtyRaised = false;
            collection.AddToolset(new Toolset("testTools", Path.GetTempPath(), collection, Path.GetTempPath()));
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.DefaultToolsVersion;
            dirtyRaised = false;
            collection.DefaultToolsVersion = "testTools";
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.Toolsets;
            dirtyRaised = false;
            collection.RemoveToolset("testTools");
            Assert.IsTrue(dirtyRaised);

            expectedChange = ProjectCollectionChangedState.Toolsets;
            dirtyRaised = false;
            collection.RemoveAllToolsets();
            Assert.IsTrue(dirtyRaised);
        }

        /// <summary>
        /// Changes to the ProjectCollection object should raise a ProjectCollectionChanged event.
        /// </summary>
        [TestMethod]
        public void ProjectCollectionChangedEvent2()
        {
            // Verify if the project, project collection and the value we are setting in the project collection are all the same
            // then the projects value for the property should not change and no event should be fired.
            ProjectCollection collection = new ProjectCollection();
            XmlReader reader = CreateProjectXmlReader();
            Project project = collection.LoadProject(reader, "4.0");
            project.SetProperty("a", "1");
            collection.SetGlobalProperty("a", "1");
            VerifyProjectCollectionEvents(collection, false, "1");

            // Verify if the project, project collection and the value we are setting in the project collection are all the same
            // then the projects value for the property should not change and no event should be fired.
            collection = new ProjectCollection();
            reader = CreateProjectXmlReader();
            project = collection.LoadProject(reader, "4.0");
            project.SetProperty("a", "%28x86%29");
            collection.SetGlobalProperty("a", "%28x86%29");
            VerifyProjectCollectionEvents(collection, false, "%28x86%29");

            // Verify if the project, project collection have the same value but a new value is set in the project collection
            // then the projects value for the property should be change and an event should be fired.
            collection = new ProjectCollection();
            reader = CreateProjectXmlReader();
            project = collection.LoadProject(reader, "4.0");
            project.SetProperty("a", "1");
            collection.SetGlobalProperty("a", "1");
            VerifyProjectCollectionEvents(collection, true, "2");
            project.GetPropertyValue("a").Equals("2", StringComparison.OrdinalIgnoreCase);

            // Verify if the project, project collection have the same value but a new value is set in the project collection
            // then the projects value for the property should be change and an event should be fired.
            collection = new ProjectCollection();
            reader = CreateProjectXmlReader();
            project = collection.LoadProject(reader, "4.0");
            project.SetProperty("a", "1");
            collection.SetGlobalProperty("a", "(x86)");
            VerifyProjectCollectionEvents(collection, true, "%28x86%29");
            project.GetPropertyValue("a").Equals("%28x86%29", StringComparison.OrdinalIgnoreCase);

            // Verify if the project has one value and project collection and the property we are setting on the project collection have the same value
            // then the projects value for the property should be change but no event should be fired
            collection = new ProjectCollection();
            reader = CreateProjectXmlReader();
            project = collection.LoadProject(reader, "4.0");
            project.SetProperty("a", "2");
            collection.SetGlobalProperty("a", "1");

            VerifyProjectCollectionEvents(collection, false, "1");
            project.GetPropertyValue("a").Equals("1", StringComparison.OrdinalIgnoreCase);

            // Verify if the project and the property being set have one value but the project collection has another
            // then the projects value for the property should not change and event should be fired
            collection = new ProjectCollection();
            reader = CreateProjectXmlReader();
            project = collection.LoadProject(reader, "4.0");
            project.SetProperty("a", "1");
            collection.SetGlobalProperty("a", "2");
            VerifyProjectCollectionEvents(collection, true, "1");
            project.GetPropertyValue("a").Equals("1", StringComparison.OrdinalIgnoreCase);

            // item is added to project collection for the first time. Make sure it is added to the project and an event is fired.
            collection = new ProjectCollection();
            reader = CreateProjectXmlReader();
            project = collection.LoadProject(reader, "4.0");

            VerifyProjectCollectionEvents(collection, true, "1");
            project.GetPropertyValue("a").Equals("1", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Changes to project XML should raise an ProjectXmlChanged event.
        /// </summary>
        [TestMethod]
        public void ProjectXmlChangedEvent()
        {
            ProjectCollection collection = new ProjectCollection();
            ProjectRootElement pre = null;
            bool dirtyRaised = false;
            collection.ProjectXmlChanged +=
                (sender, e) =>
                {
                    Assert.AreSame(collection, sender);
                    Assert.AreSame(pre, e.ProjectXml);
                    this.TestContext.WriteLine(e.Reason ?? String.Empty);
                    dirtyRaised = true;
                };
            Assert.IsFalse(dirtyRaised);

            // Ensure that the event is raised even when DisableMarkDirty is set.
            collection.DisableMarkDirty = true;

            // Create a new PRE but don't change the template.
            dirtyRaised = false;
            pre = ProjectRootElement.Create(collection);
            Assert.IsFalse(dirtyRaised);

            // Change PRE prior to setting a filename and thus associating the PRE with the ProjectCollection.
            dirtyRaised = false;
            pre.AppendChild(pre.CreatePropertyGroupElement());
            Assert.IsFalse(dirtyRaised);

            // Associate with the ProjectCollection
            dirtyRaised = false;
            pre.FullPath = FileUtilities.GetTemporaryFile();
            Assert.IsTrue(dirtyRaised);

            // Now try dirtying again and see that the event is raised this time.
            dirtyRaised = false;
            pre.AppendChild(pre.CreatePropertyGroupElement());
            Assert.IsTrue(dirtyRaised);

            // Make sure that project collection global properties don't raise this event.
            dirtyRaised = false;
            collection.SetGlobalProperty("a", "b");
            Assert.IsFalse(dirtyRaised);

            // Change GlobalProperties on a project to see that that doesn't propagate as an XML change.
            dirtyRaised = false;
            var project = new Project(pre);
            project.SetGlobalProperty("q", "s");
            Assert.IsFalse(dirtyRaised);

            // Change XML via the Project to verify the event is raised.
            dirtyRaised = false;
            project.SetProperty("z", "y");
            Assert.IsTrue(dirtyRaised);
        }

        /// <summary>
        /// Changes to a Project evaluation object should raise a ProjectChanged event.
        /// </summary>
        [TestMethod]
        public void ProjectChangedEvent()
        {
            ProjectCollection collection = new ProjectCollection();
            ProjectRootElement pre = null;
            Project project = null;
            bool dirtyRaised = false;
            collection.ProjectChanged +=
                (sender, e) =>
                {
                    Assert.AreSame(collection, sender);
                    Assert.AreSame(project, e.Project);
                    dirtyRaised = true;
                };
            Assert.IsFalse(dirtyRaised);

            pre = ProjectRootElement.Create(collection);
            project = new Project(pre, null, null, collection);

            // all these should still pass with disableMarkDirty set
            collection.DisableMarkDirty = true;
            project.DisableMarkDirty = true;

            dirtyRaised = false;
            pre.AppendChild(pre.CreatePropertyGroupElement());
            Assert.IsFalse(dirtyRaised, "Dirtying the XML directly should not result in a ProjectChanged event.");

            // No events should be raised before we associate a filename with the PRE
            dirtyRaised = false;
            project.SetGlobalProperty("someGlobal", "someValue");
            Assert.IsFalse(dirtyRaised);

            dirtyRaised = false;
            project.SetProperty("someProp", "someValue");
            Assert.IsFalse(dirtyRaised);

            pre.FullPath = FileUtilities.GetTemporaryFile();
            dirtyRaised = false;
            project.SetGlobalProperty("someGlobal", "someValue2");
            Assert.IsTrue(dirtyRaised);

            dirtyRaised = false;
            project.RemoveGlobalProperty("someGlobal");
            Assert.IsTrue(dirtyRaised);

            dirtyRaised = false;
            collection.SetGlobalProperty("somePCglobal", "someValue");
            Assert.IsTrue(dirtyRaised);

            dirtyRaised = false;
            project.SetProperty("someProp", "someValue2");
            Assert.IsTrue(dirtyRaised);
        }

        /// <summary>
        /// Create an empty project file and return the path
        /// </summary>
        private static string CreateProjectFile()
        {
            ProjectRootElement xml = ProjectRootElement.Create();
            string path = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
            xml.Save(path);
            return path;
        }

        /// <summary>
        /// Create an XmlReader around an empty project file content
        /// </summary>
        private XmlReader CreateProjectXmlReader()
        {
            ProjectRootElement xml = ProjectRootElement.Create();
            XmlReader reader = XmlReader.Create(new StringReader(xml.RawXml));
            return reader;
        }

        /// <summary>
        /// Verify that when a property is set on the project collection that the correct events are fired.
        /// </summary>
        private void VerifyProjectCollectionEvents(ProjectCollection collection, bool expectEventRaised, string propertyValue)
        {
            bool raisedEvent = false;
            ProjectCollectionChangedState expectedChange = ProjectCollectionChangedState.Loggers;
            collection.ProjectCollectionChanged +=
                (sender, e) =>
                {
                    Assert.AreSame(collection, sender);
                    Assert.AreEqual(expectedChange, e.Changed);
                    raisedEvent = true;
                };

            expectedChange = ProjectCollectionChangedState.GlobalProperties;
            collection.SetGlobalProperty("a", propertyValue);
            Assert.AreEqual(raisedEvent, expectEventRaised);
            ProjectPropertyInstance property = collection.GetGlobalProperty("a");
            Assert.IsNotNull(property);
            Assert.IsTrue(String.Equals(property.EvaluatedValue, ProjectCollection.Unescape(propertyValue), StringComparison.OrdinalIgnoreCase));
        }
    }
}
