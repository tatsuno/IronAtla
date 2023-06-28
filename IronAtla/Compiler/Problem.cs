namespace IronAtla.Compiler
{
    public class Problem
    {
        public enum Severity
        {
            ERROR, WARNING, INFO
        }

        public readonly Severity severity;
        public readonly string message;

        public Problem(Severity severity, string message)
        {
            this.severity = severity;
            this.message = message;
        }
    }
}
