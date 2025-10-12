using System.Linq.Expressions;
using SquidCraft.Entities.Models.Base;

namespace SquidCraft.Entities.Interfaces;

public interface IEntityDataAccess<TEntity> where TEntity : BaseEntity
{
    Task<List<TEntity>> GetAllAsync();

    Task<TEntity?> GetByIdAsync(Guid id);

    Task<TEntity> InsertAsync(TEntity entity);

    Task<TEntity> UpdateAsync(TEntity entity);

    Task<bool> DeleteAsync(Guid id);

    Task<long> CountAsync();


    Task<IEnumerable<TEntity>> SearchAsync(Expression<Func<TEntity, bool>> predicate);

    Task<TEntity?> SearchSingleAsync(Expression<Func<TEntity, bool>> predicate);
}
