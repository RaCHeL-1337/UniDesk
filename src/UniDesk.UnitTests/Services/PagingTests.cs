using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using UniDesk.Web.Models;

namespace UniDesk.UnitTests.Services
{
    public class PagingTests
    {
        [Fact]
        public void Paging_ShouldSkipCorrectNumber_WhenPage2()
        {
            var tickets = Enumerable.Range(1, 15)
                .Select(i => new Ticket
                {
                    Title = "T",
                    Description = "D"
                })
                .AsQueryable();

            int page = 2;
            int pageSize = 10;

            var result = tickets
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            Assert.Equal(5, result.Count);
        }
    }
}
