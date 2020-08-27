﻿namespace Coderr.Client.Tests.Uploaders.TestObjects
{
    public class ClassWithPrivateSetter
    {
        public ClassWithPrivateSetter(string prop)
        {
            Prop = prop;
        }

        protected ClassWithPrivateSetter()
        {
            
        }

        public string Prop { get; private set; }
    }
}
