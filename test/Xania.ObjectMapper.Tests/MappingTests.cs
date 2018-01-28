using System;
using System.Collections;
using System.Collections.Generic;
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
            1.MapTo<double>().Should().Be(1d);
            1.MapTo<sbyte>().Should().Be(1);
            1.MapTo<byte>().Should().Be(1);
            1.MapTo<long>().Should().Be(1L);
        }

        [Test]
        public void MapToNullable()
        {
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
            var graphSON = new GraphSON();
            graphSON.Values.Add("firstName", "Ibrahim");
            graphSON.Values.Add("lastName", "ben Salah");
            graphSON.Values.Add("parent", new { firstName = "MFadel" });

            var person = graphSON.MapTo<Person>();
            person.FirstName.Should().Be("Ibrahim");
            person.LastName.Should().Be("ben Salah");
            person.Parent.FirstName.Should().Be("MFadel");
        }

        private class GraphSON: IMap<string, object>
        {
            public IDictionary<string, object> Values { get; } = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            public bool TryGetValue(string name, out object value)
            {
                return Values.TryGetValue(name, out value);
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return Values.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}