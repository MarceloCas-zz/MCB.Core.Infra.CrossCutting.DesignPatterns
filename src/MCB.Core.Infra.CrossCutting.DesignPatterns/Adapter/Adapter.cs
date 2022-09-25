using MapsterMapper;
using MCB.Core.Infra.CrossCutting.DesignPatterns.Abstractions.Adapter;

namespace MCB.Core.Infra.CrossCutting.DesignPatterns.Adapter;

public class Adapter
    : IAdapter
{
    // Fields
    private readonly IMapper _mapper;

    // Constructors
    public Adapter(IMapper mapper)
    {
        _mapper = mapper;
    }

    // Public Methods
    public TTarget Adapt<TSource, TTarget>(TSource source)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        return _mapper.Map<TTarget>(source);
    }
    public TTarget Adapt<TSource, TTarget>(TSource source, TTarget existingTarget)
    {
        if(existingTarget is null)
            return Adapt<TSource, TTarget>(source);
        else
            return _mapper.Map(source, existingTarget);
    }
}
