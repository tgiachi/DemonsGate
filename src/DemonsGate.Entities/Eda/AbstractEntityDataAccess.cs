using System.Linq.Expressions;
using DemonsGate.Core.Directories;
using DemonsGate.Core.Enums;
using DemonsGate.Entities.Attributes;
using DemonsGate.Entities.Interfaces;
using DemonsGate.Entities.Models.Base;
using MemoryPack;
using Serilog;

namespace DemonsGate.Entities.Eda;

public class AbstractEntityDataAccess<TEntity> : IEntityDataAccess<TEntity>, IDisposable where TEntity : BaseEntity
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _filePath;
    private readonly ILogger _logger = Log.ForContext<AbstractEntityDataAccess<TEntity>>();

    public AbstractEntityDataAccess(DirectoriesConfig directoriesConfig)
    {
        // Get the EntityAttribute from TEntity

        if (typeof(TEntity).GetCustomAttributes(typeof(EntityAttribute), false)
                .FirstOrDefault() is not EntityAttribute entityAttribute)
        {
            throw new InvalidOperationException($"Entity {typeof(TEntity).Name} must have an EntityAttribute");
        }

        // Resolve the file path using DirectoriesConfig
        var entitiesDirectory = directoriesConfig[DirectoryType.Database];
        _filePath = Path.Combine(entitiesDirectory, entityAttribute.DefaultFileName);

        _logger.Debug("Initialized {EntityType} data access with file path: {FilePath}", typeof(TEntity).Name, _filePath);
    }

    private async Task<List<TEntity>> LoadEntitiesAsync()
    {
        if (!File.Exists(_filePath))
        {
            _logger.Debug("File {FilePath} does not exist, returning empty list", _filePath);
            return [];
        }

        _logger.Debug("Loading entities from {FilePath}", _filePath);
        await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var entities = await MemoryPackSerializer.DeserializeAsync<List<TEntity>>(stream);
        _logger.Debug("Loaded {Count} entities from {FilePath}", entities?.Count ?? 0, _filePath);
        return entities ?? [];
    }

    private async Task SaveEntitiesAsync(List<TEntity> entities)
    {
        _logger.Debug("Saving {Count} entities to {FilePath}", entities.Count, _filePath);
        await using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await MemoryPackSerializer.SerializeAsync(stream, entities);
        _logger.Debug("Successfully saved entities to {FilePath}", _filePath);
    }

    public async Task<List<TEntity>> GetAllAsync()
    {
        _logger.Debug("Getting all entities");
        await _lock.WaitAsync();
        try
        {
            var entities = await LoadEntitiesAsync();
            _logger.Debug("Retrieved {Count} entities", entities.Count);
            return entities;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<TEntity?> GetByIdAsync(Guid id)
    {
        _logger.Debug("Getting entity by ID: {Id}", id);
        await _lock.WaitAsync();
        try
        {
            var entities = await LoadEntitiesAsync();
            var entity = entities.FirstOrDefault(e => e.Id == id);
            _logger.Debug("Entity {Id} {Found}", id, entity != null ? "found" : "not found");
            return entity;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<TEntity> InsertAsync(TEntity entity)
    {
        _logger.Debug("Creating new entity");
        await _lock.WaitAsync();
        try
        {
            var entities = await LoadEntitiesAsync();

            entity.Id = Guid.NewGuid();
            entity.Created = DateTime.UtcNow;
            entity.Updated = DateTime.UtcNow;

            entities.Add(entity);
            await SaveEntitiesAsync(entities);

            _logger.Debug("Created entity with ID: {Id}", entity.Id);
            return entity;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        _logger.Debug("Updating entity with ID: {Id}", entity.Id);
        await _lock.WaitAsync();
        try
        {
            var entities = await LoadEntitiesAsync();
            var index = entities.FindIndex(e => e.Id == entity.Id);

            if (index == -1)
            {
                _logger.Debug("Entity with ID {Id} not found for update", entity.Id);
                throw new InvalidOperationException($"Entity with ID {entity.Id} not found");
            }

            entity.Updated = DateTime.UtcNow;
            entities[index] = entity;

            await SaveEntitiesAsync(entities);

            _logger.Debug("Successfully updated entity with ID: {Id}", entity.Id);
            return entity;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        _logger.Debug("Deleting entity with ID: {Id}", id);
        await _lock.WaitAsync();
        try
        {
            var entities = await LoadEntitiesAsync();
            var removed = entities.RemoveAll(e => e.Id == id);

            if (removed > 0)
            {
                await SaveEntitiesAsync(entities);
                _logger.Debug("Successfully deleted entity with ID: {Id}", id);
                return true;
            }

            _logger.Debug("Entity with ID {Id} not found for deletion", id);
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<long> CountAsync()
    {
        _logger.Debug("Counting entities");
        await _lock.WaitAsync();
        try
        {
            var entities = await LoadEntitiesAsync();
            var count = entities.Count;
            _logger.Debug("Entity count: {Count}", count);
            return count;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IEnumerable<TEntity>> SearchAsync(Expression<Func<TEntity, bool>> predicate)
    {
        _logger.Debug("Searching entities with predicate");
        await _lock.WaitAsync();
        try
        {
            var entities = await LoadEntitiesAsync();
            var results = entities.Where(predicate.Compile()).ToList();
            _logger.Debug("Search returned {Count} entities", results.Count);
            return results;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<TEntity?> SearchSingleAsync(Expression<Func<TEntity, bool>> predicate)
    {
        _logger.Debug("Searching single entity with predicate");
        await _lock.WaitAsync();
        try
        {
            var entities = await LoadEntitiesAsync();
            var entity = entities.FirstOrDefault(predicate.Compile());
            _logger.Debug("Single search {Found}", entity != null ? "found entity" : "returned null");
            return entity;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
        _logger.Debug("Disposed {EntityType} data access", typeof(TEntity).Name);
        GC.SuppressFinalize(this);
    }
}
