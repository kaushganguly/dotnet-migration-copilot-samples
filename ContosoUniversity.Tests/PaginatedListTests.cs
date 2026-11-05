using ContosoUniversity;

namespace ContosoUniversity.Tests
{
    public class PaginatedListTests
    {
        [Fact]
        public void Create_ReturnsFirstPage_WhenPageIndexIsOne()
        {
            var source = Enumerable.Range(1, 25).AsQueryable();

            var page = PaginatedList<int>.Create(source, pageIndex: 1, pageSize: 10);

            Assert.Equal(10, page.Count);
            Assert.Equal(1, page.PageIndex);
            Assert.Equal(3, page.TotalPages);
            Assert.False(page.HasPreviousPage);
            Assert.True(page.HasNextPage);
            Assert.Equal(1, page.First());
            Assert.Equal(10, page.Last());
        }

        [Fact]
        public void Create_ReturnsLastPartialPage_WhenOnFinalPage()
        {
            var source = Enumerable.Range(1, 25).AsQueryable();

            var page = PaginatedList<int>.Create(source, pageIndex: 3, pageSize: 10);

            Assert.Equal(5, page.Count);
            Assert.Equal(3, page.TotalPages);
            Assert.True(page.HasPreviousPage);
            Assert.False(page.HasNextPage);
        }

        [Fact]
        public void Create_ReturnsEmptyPage_WhenSourceIsEmpty()
        {
            var source = Enumerable.Empty<int>().AsQueryable();

            var page = PaginatedList<int>.Create(source, pageIndex: 1, pageSize: 10);

            Assert.Empty(page);
            Assert.Equal(0, page.TotalPages);
            Assert.False(page.HasPreviousPage);
            Assert.False(page.HasNextPage);
        }
    }
}
