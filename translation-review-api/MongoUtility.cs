using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace translation_review_api
{
    public static class MongoUtility
    {
        public static void RegisterConventions()
        {
            var conventionPack = new ConventionPack
        {
            new CamelCaseElementNameConvention()
        };
            ConventionRegistry.Register("CamelCaseConvention", conventionPack, type => true);
        }
    }
}
