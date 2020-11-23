using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleNamespace
{
    class SimpleClass
    {
        private void PrivateMeth() { }
        public void VoidMeth(int k, float b) { }

        public char CharMeth(string s)
        {
            return 'F';
        }
    }
}
namespace HarderNamespace
{
    namespace AbsractName
    {
        abstract class Person
        {
            public string Name { get; set; }

            public Person(string name)
            {
                Name = name;
            }

            public void Display()
            {
                Console.WriteLine(Name);
            }
        }

    }
    namespace InjectClass
    {
        class ParanetClass
        {
            class ChildClass
            {
                public void VoidMeth() { }

                public char CharMeth()
                {
                    return 'F';
                }
            }
        }
    }
    namespace StaticName
    {
        static class StaticClass
        {
            public static void VoidMeth(bool Bool, int k)
            {

            }

            public static bool BoolMeth(string s)
            {
                return true;
            }
        }
        class TestClass { }
        class ClassWithStaticMeth
        {
            public TestClass ClassMeth(int j)
            {
                return null;
            }

            public static int GetInt(float g, string asd)
            {
                return 322;
            }
        }
    }
    namespace InterfaceName
    {
        class TestClass { }
        interface ITestInt { }
        interface IMoreInt { }
        class ClassWithInter
        {
            public ClassWithInter(int i, ITestInt testInt)
            {
            }
            private void PrivateMeth() { }
            public void VoidMeth(int k, float b, ITestInt test) { }

            public char CharMeth(TestClass test)
            {
                return 'F';
            }
        }
        class ClassWithTwoInter
        {
            public ClassWithTwoInter(int i, ITestInt testInt, IMoreInt moreInt)
            {
            }

            public char CharMeth(TestClass test)
            {
                return 'F';
            }
        }
    }
}
