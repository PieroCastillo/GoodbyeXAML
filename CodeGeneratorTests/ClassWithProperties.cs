namespace CodeGeneratorTests
{
    public class ClassWithProperties
    {
        // Should get extension methods
        public string StringProperty { get; set; }
        public int IntProperty { get; set; }

        // Should NOT get extension methods
        public int NoSetter { get; }
        public int PrivateSetter { get; private set; }
        private int PrivateProperty { get; set; } 
    }
}