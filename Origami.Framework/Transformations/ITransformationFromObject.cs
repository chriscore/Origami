using System.Collections.Generic;

namespace Origami.Framework.Transformations
{
    public interface ITransformationFromObject
    {
        object Transform(Dictionary<string, object> settings, object input);
    }
}
