using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tanneryd.BulkOperations.EF6.Model;
using Tanneryd.BulkOperations.EF6.Tests.DM.Blog;
using Tanneryd.BulkOperations.EF6.Tests.EF;

namespace Tanneryd.BulkOperations.EF6.Tests
{
    [TestClass]
    public class BulkUpdateTests : BulkOperationTestBase
    {
        [TestInitialize]
        public void Initialize()
        {
            CleanupBlogContext();
            InitializeBlogContext();
        }

        [TestCleanup]
        public void CleanUp()
        {
            CleanupBlogContext();
        }

        [TestMethod]
        public void ModifiedEntityShouldBeUpdated()
        {
            using (var db = new BlogContext())
            {
                var blog = new Blog { Name = "My Blog" };
                var firstPost = new Post
                {
                    Text = "My first blogpost.",
                    PostKeywords = new List<Keyword>() { new Keyword { Text = "first" } }
                };
                var secondPost = new Post
                {
                    Text = "My second blogpost.",
                    PostKeywords = new List<Keyword>() { new Keyword { Text = "second" } }
                };
                blog.BlogPosts.Add(firstPost);
                blog.BlogPosts.Add(secondPost);
                var req = new BulkInsertRequest<Blog>
                {
                    Entities = new[] { blog }.ToList(),
                    AllowNotNullSelfReferences = false,
                    SortUsingClusteredIndex = true,
                    Recursive = true
                };
                var response = db.BulkInsertAll(req);
                var b = db.Blogs.Single();
                Assert.AreEqual("My Blog", b.Name);
                Assert.AreEqual(2, b.BlogPosts.Count);
                //Assert.AreEqual("My second blogpost.", b.BlogPosts.First(p=>p.PostKeywords.Any(k=>k.Text== "second")).Text);

            }
        }
      
        [TestMethod]
        public void ModifiedEntityShouldBeUpdatedWithInsert()
        {
            using (var db = new BlogContext())
            {
                var blog = new Blog { Name = "My Blog1" };
                db.Blogs.Add(blog);
                db.SaveChanges();
                              
                var blog2 = new Blog { Name = "My Blog2",Id=Guid.NewGuid()};
                db.BulkUpdateAll(new BulkUpdateRequest
                {
                    Entities = new[] { blog, blog2 },
                    GetUpdateStatement = (name) =>
                    {
                        return $"t1.[{name}]+'(updated)'";
                    },
                    GetInsertStatement = (name) =>
                    {
                        return $"t1.[{name}]+'(Inserted)'";
                    },
                    InsertIfNew = true
                });
                RefreshAll(db);
                Assert.AreEqual(db.Blogs.Count(), 2);
                var b1 = db.Blogs.First(p=>p.Id== blog.Id);
                Assert.AreEqual(b1.Name,"My Blog1(updated)");
                var b2 = db.Blogs.First(p => p.Id != blog.Id);
                Assert.AreEqual(b2.Name, "My Blog2(Inserted)");

            }
        }
    }
}