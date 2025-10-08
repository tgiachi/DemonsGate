using DemonsGate.Core.Directories;
using DemonsGate.Entities.Eda;

namespace DemonsGate.Tests.Entities;

[TestFixture]
public class AbstractEntityDataAccessTests
{
    private DirectoriesConfig _directoriesConfig = null!;
    private AbstractEntityDataAccess<TestEntity> _dataAccess = null!;
    private string _testDirectory = null!;

    [SetUp]
    public void SetUp()
    {
        // Create a temporary directory for tests
        _testDirectory = Path.Combine(Path.GetTempPath(), "DemonsGateTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);

        // Initialize DirectoriesConfig with test directory
        _directoriesConfig = new DirectoriesConfig(_testDirectory, ["Database"]);
        _dataAccess = new AbstractEntityDataAccess<TestEntity>(_directoriesConfig);
    }

    [TearDown]
    public void TearDown()
    {
        // Dispose the data access
        _dataAccess?.Dispose();

        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Test]
    public async Task CreateAsync_ShouldCreateNewEntity()
    {
        // Arrange
        var entity = new TestEntity
        {
            Name = "Test Entity",
            Value = 42
        };

        // Act
        var result = await _dataAccess.CreateAsync(entity);

        // Assert
        Assert.That(result.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(result.Name, Is.EqualTo("Test Entity"));
        Assert.That(result.Value, Is.EqualTo(42));
        Assert.That(result.Created, Is.Not.EqualTo(default(DateTime)));
        Assert.That(result.Updated, Is.Not.EqualTo(default(DateTime)));
        Assert.That(result.Created, Is.EqualTo(result.Updated));
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoEntities()
    {
        // Act
        var result = await _dataAccess.GetAllAsync();

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllEntities()
    {
        // Arrange
        await _dataAccess.CreateAsync(new TestEntity { Name = "Entity 1", Value = 1 });
        await _dataAccess.CreateAsync(new TestEntity { Name = "Entity 2", Value = 2 });
        await _dataAccess.CreateAsync(new TestEntity { Name = "Entity 3", Value = 3 });

        // Act
        var result = await _dataAccess.GetAllAsync();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnEntity_WhenExists()
    {
        // Arrange
        var created = await _dataAccess.CreateAsync(new TestEntity { Name = "Test", Value = 100 });

        // Act
        var result = await _dataAccess.GetByIdAsync(created.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Id, Is.EqualTo(created.Id));
        Assert.That(result.Name, Is.EqualTo("Test"));
        Assert.That(result.Value, Is.EqualTo(100));
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Act
        var result = await _dataAccess.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task UpdateAsync_ShouldUpdateEntity()
    {
        // Arrange
        var created = await _dataAccess.CreateAsync(new TestEntity { Name = "Original", Value = 1 });
        var originalUpdated = created.Updated;

        // Wait a bit to ensure Updated timestamp changes
        await Task.Delay(10);

        created.Name = "Modified";
        created.Value = 999;

        // Act
        var result = await _dataAccess.UpdateAsync(created);

        // Assert
        Assert.That(result.Name, Is.EqualTo("Modified"));
        Assert.That(result.Value, Is.EqualTo(999));
        Assert.That(result.Updated, Is.GreaterThan(originalUpdated));

        // Verify persistence
        var retrieved = await _dataAccess.GetByIdAsync(created.Id);
        Assert.That(retrieved!.Name, Is.EqualTo("Modified"));
        Assert.That(retrieved.Value, Is.EqualTo(999));
    }

    [Test]
    public async Task UpdateAsync_ShouldThrowException_WhenEntityNotFound()
    {
        // Arrange
        var entity = new TestEntity
        {
            Id = Guid.NewGuid(),
            Name = "Non-existent",
            Value = 0
        };

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await _dataAccess.UpdateAsync(entity));
    }

    [Test]
    public async Task DeleteAsync_ShouldDeleteEntity_WhenExists()
    {
        // Arrange
        var created = await _dataAccess.CreateAsync(new TestEntity { Name = "To Delete", Value = 1 });

        // Act
        var result = await _dataAccess.DeleteAsync(created.Id);

        // Assert
        Assert.That(result, Is.True);

        // Verify deletion
        var retrieved = await _dataAccess.GetByIdAsync(created.Id);
        Assert.That(retrieved, Is.Null);
    }

    [Test]
    public async Task DeleteAsync_ShouldReturnFalse_WhenEntityNotExists()
    {
        // Act
        var result = await _dataAccess.DeleteAsync(Guid.NewGuid());

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task CountAsync_ShouldReturnZero_WhenNoEntities()
    {
        // Act
        var count = await _dataAccess.CountAsync();

        // Assert
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        await _dataAccess.CreateAsync(new TestEntity { Name = "Entity 1", Value = 1 });
        await _dataAccess.CreateAsync(new TestEntity { Name = "Entity 2", Value = 2 });
        await _dataAccess.CreateAsync(new TestEntity { Name = "Entity 3", Value = 3 });

        // Act
        var count = await _dataAccess.CountAsync();

        // Assert
        Assert.That(count, Is.EqualTo(3));
    }

    [Test]
    public async Task SearchAsync_ShouldReturnMatchingEntities()
    {
        // Arrange
        await _dataAccess.CreateAsync(new TestEntity { Name = "Apple", Value = 1 });
        await _dataAccess.CreateAsync(new TestEntity { Name = "Banana", Value = 2 });
        await _dataAccess.CreateAsync(new TestEntity { Name = "Apple Pie", Value = 3 });

        // Act
        var result = await _dataAccess.SearchAsync(e => e.Name.Contains("Apple"));

        // Assert
        var entities = result.ToList();
        Assert.That(entities, Has.Count.EqualTo(2));
        Assert.That(entities.All(e => e.Name.Contains("Apple")), Is.True);
    }

    [Test]
    public async Task SearchAsync_ShouldReturnEmpty_WhenNoMatches()
    {
        // Arrange
        await _dataAccess.CreateAsync(new TestEntity { Name = "Test", Value = 1 });

        // Act
        var result = await _dataAccess.SearchAsync(e => e.Name == "NonExistent");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task SearchSingleAsync_ShouldReturnEntity_WhenMatches()
    {
        // Arrange
        await _dataAccess.CreateAsync(new TestEntity { Name = "Unique", Value = 999 });

        // Act
        var result = await _dataAccess.SearchSingleAsync(e => e.Value == 999);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Name, Is.EqualTo("Unique"));
        Assert.That(result.Value, Is.EqualTo(999));
    }

    [Test]
    public async Task SearchSingleAsync_ShouldReturnNull_WhenNoMatch()
    {
        // Arrange
        await _dataAccess.CreateAsync(new TestEntity { Name = "Test", Value = 1 });

        // Act
        var result = await _dataAccess.SearchSingleAsync(e => e.Value == 999);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task DataPersistence_ShouldPersistAcrossInstances()
    {
        // Arrange - Create entity with first instance
        var entity = await _dataAccess.CreateAsync(new TestEntity { Name = "Persistent", Value = 42 });
        _dataAccess.Dispose();

        // Act - Create new instance and retrieve
        _dataAccess = new AbstractEntityDataAccess<TestEntity>(_directoriesConfig);
        var retrieved = await _dataAccess.GetByIdAsync(entity.Id);

        // Assert
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("Persistent"));
        Assert.That(retrieved.Value, Is.EqualTo(42));
    }

    [Test]
    public async Task ConcurrentOperations_ShouldBeThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Create 10 entities concurrently
        for (int i = 0; i < 10; i++)
        {
            var index = i;
            tasks.Add(Task.Run(async () =>
            {
                await _dataAccess.CreateAsync(new TestEntity { Name = $"Entity {index}", Value = index });
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var count = await _dataAccess.CountAsync();
        Assert.That(count, Is.EqualTo(10));

        var allEntities = await _dataAccess.GetAllAsync();
        Assert.That(allEntities.Select(e => e.Value).Distinct().Count(), Is.EqualTo(10));
    }
}
