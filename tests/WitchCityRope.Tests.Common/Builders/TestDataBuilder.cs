using Bogus;

namespace WitchCityRope.Tests.Common.Builders
{
    /// <summary>
    /// Base class for test data builders using the Builder pattern
    /// </summary>
    public abstract class TestDataBuilder<TEntity, TBuilder>
        where TEntity : class
        where TBuilder : TestDataBuilder<TEntity, TBuilder>
    {
        protected readonly Faker _faker;

        protected TestDataBuilder()
        {
            _faker = new Faker();
        }

        /// <summary>
        /// Builds the entity with the configured values
        /// </summary>
        public abstract TEntity Build();

        /// <summary>
        /// Builds multiple entities
        /// </summary>
        public List<TEntity> BuildMany(int count)
        {
            var entities = new List<TEntity>();
            for (int i = 0; i < count; i++)
            {
                entities.Add(Build());
            }
            return entities;
        }

        /// <summary>
        /// Returns this instance as the derived type for fluent chaining
        /// </summary>
        protected TBuilder This => (TBuilder)this;
    }
}