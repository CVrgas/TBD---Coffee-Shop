namespace Domain.Base.Entities;

public interface IHasRowVersion
{
    byte[] RowVersion { get;}
}