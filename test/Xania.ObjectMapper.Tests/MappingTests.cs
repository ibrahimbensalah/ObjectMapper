using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Xania.ObjectMapper.Tests
{
    public class MappingTests
    {
        [Test]
        public void MapStringToInt()
        {
            var str = "123";
            str.MapTo<int>().Should().Be(123);
        }

        [Test]
        public void MapToDateTime()
        {
            var now = DateTime.Now;
            var str = now.ToString("O");
            str.MapTo<DateTime>().Should().Be(now);
        }

        [Test]
        public void MapToDateTimeOffset()
        {
            var now = DateTimeOffset.Now;
            var str = now.ToString("O");
            str.MapTo<DateTimeOffset>().Should().Be(now);
        }

        [Test]
        public void MapToPrimitive()
        {
            1.MapTo<float>().Should().Be(1f);
            "1".MapTo<decimal>().Should().Be(1m);
            1m.MapTo<double>().Should().Be(1d);
            1.MapTo<sbyte>().Should().Be(1);
            1.MapTo<byte>().Should().Be(1);
            1.MapTo<long>().Should().Be(1L);
        }

        [Test]
        public void MapToNullable()
        {
            int? nill = null;
            nill.MapTo<int?>().Should().BeNull();
            1.MapTo<float?>().Should().Be(1f);

            DateTime? now = DateTime.Now;
            now.MapTo<DateTime?>().Should().Be(now);
        }

        [Test]
        public void MapDictionaryToObject()
        {
            var dict = new Dictionary<string, object>
            {
                {"firstName", "Ibrahim"},
                {"lastName", "ben Salah"},
                {"parent", new { firstName = "MFadel" }}
            };
            var person = dict.MapTo<Person>();
            person.FirstName.Should().Be("Ibrahim");
            person.LastName.Should().Be("ben Salah");
            person.Parent.FirstName.Should().Be("MFadel");
        }

        [Test]
        public void MapObjectToObject()
        {
            var obj = new
            {
                firstName = "Ibrahim",
                lastName = "ben Salah",
                Parent = new
                {
                    firstName = "Mfadel"
                }
            };
            var person = obj.MapTo<Person>();
            person.FirstName.Should().Be(obj.firstName);
            person.LastName.Should().Be(obj.lastName);
            person.Parent.FirstName.Should().Be(obj.Parent.firstName);
        }

        [Test]
        public void CustomMapToObject()
        {
            // arrange 
            var mapper = new Mapper(new GraphSONMappingResolver());
            var g = new GraphSON();
            g.Properties.Add("firstName", "Ibrahim");
            g.Properties.Add("lastName", "ben Salah");
            g.Edges.Add("parent", new { firstName = "MFadel" });

            // act 
            var person = mapper.MapTo<Person>(g);

            // assert
            person.FirstName.Should().Be("Ibrahim");
            person.LastName.Should().Be("ben Salah");
            person.Parent.FirstName.Should().Be("MFadel");
        }

        [Test]
        public void MapToDynamicType()
        {
            var obj = new
            {
                firstName = 123,
                Parent = new Contact()
            };

            var mapper = new Mapper(new PersonConstract());
            var person = mapper.MapTo<Person>(obj);

            person.FirstName.Should().Be("Ibrahim 123");
        }

        [Test]
        public void MapToEnumerable()
        {
            var obj = new
            {
                numbers = new [] {1m, 2m, 3m},
                names = new[] { "1", "2", "3"}
            };
            var contact = obj.MapTo<Contact>();
            contact.Numbers.ShouldAllBeEquivalentTo(obj.numbers);
        }

        [Test]
        public void MapToPrivateCollection()
        {
            var obj = new
            {
                items = new[] { 1, 2, 3 },
            };
            var container = obj.MapTo<Container>();
            container.Items.ShouldAllBeEquivalentTo(obj.items);
        }

        [Test]
        public void CircularTest()
        {
            var mFaddal = new Person
            {
                FirstName = "MFaddel"
            };
            var ibrahim = new Person
            {
                FirstName = "Ibrahim",
                Parent = mFaddal
            };
            mFaddal.Child = ibrahim;

            var result = mFaddal.MapTo<Person>();
            result.Should().Be(result.Child.Parent);
        }

    }

    class GraphSON
    {
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        public IDictionary<string, object> Edges { get; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
    }

    class Contact
    {
        public int[] Numbers { get; set; }
        public IEnumerable<int> Names { get; set; }
    }

    class Container
    {
        public ICollection<int> Items { get; } = new List<int>();
    }

    class GraphSONMappingResolver : IMappingResolver
    {
        IOption<IMappable> IMappingResolver.Resolve(object obj)
        {
            if (obj is GraphSON g)
                return new MappableDictionary(g.Properties.Concat(g.Edges)).Some();

            return Option<IMappable>.None();
        }
    }

    class PersonConstract: IMappingResolver
    {
        public IOption<IMappable> Resolve(object obj)
        {
            if (obj is int i)
                return new MappablePrimitive($"Ibrahim {i}").Some();

            return Option<IMappable>.None();
        }
    }
}