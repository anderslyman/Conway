using AutoMapper;

namespace Conway.Library.Configuration
{
    public static class AutomapperSetup
    {
        public static MapperConfiguration SetupMappings()
        {
            return new MapperConfiguration(cfg =>
            {
                var assembly = typeof(AutomapperSetup).Assembly;
                var modelTypes = assembly.GetTypes().Where(t => t.Name.EndsWith("Model") && !t.IsNestedPrivate);
                var dtoTypes = assembly.GetTypes().Where(t => !t.IsNestedPrivate && t.Name.EndsWith("Dto"));

                /* Rather than configure every model/dto pair explicitly, let's configure by convention
                 * If a model type needs special handling, we can handle those explicitly later.
                 */
                foreach (var modelType in modelTypes)
                {
                    var baseName = modelType.Name.Replace("Model", string.Empty);
                    var matchingDto = dtoTypes.FirstOrDefault(dtoType => dtoType.Name.Replace("Dto", string.Empty) == baseName);
                    if (matchingDto == null)
                    {
                        continue;
                    }

                    cfg.CreateMap(modelType, matchingDto);
                    cfg.CreateMap(matchingDto, modelType);
                }
            });
        }

        public static IEnumerable<U> MapEnumerable<T, U>(this IMapper mapper, IEnumerable<T> collection)
        {
            return mapper.Map<IEnumerable<T>, IEnumerable<U>>(collection);
        }

        public static U[] MapEnumerableToArray<T, U>(this IMapper mapper, IEnumerable<T> collection)
        {
            return mapper.Map<IEnumerable<T>, IEnumerable<U>>(collection).ToArray();
        }
    }
}
