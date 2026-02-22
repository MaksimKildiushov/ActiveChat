using Libraries.Abstractions.Interfaces;
using Shared.Extensions;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ac.Domain.Entities.Abstract;

public abstract class BaseEntity : IBaseEntity
{
    private List<string>? _props;

    public Guid AuthorId { get; set; }

    [ForeignKey(nameof(AuthorId))]
    public UserEntity Author { get; set; } = null!;

    public DateTime Created { get; set; }

    public DateTime? Modified { get; set; }

    public Guid? ModifierId { get; set; }

    [ForeignKey(nameof(ModifierId))]
    public UserEntity? Modifier { get; set; }

    [NotMapped]
    public List<string> AllProperties
    {
        get => _props == null
            ? _props = PropertiesHlp.AllAndValues(this)
            : _props;
    }
}
