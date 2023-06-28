using System.Collections.Generic;

namespace IronAtla.Compiler
{
    public class AtlaQuarrier
    {
        private string[] labels =
        {
            "val ",
        };

        public LinkedList<Block> Quarry()
        {
            var list = new LinkedList<Block>();

            return list;
        }
    }

    public class Block
    {
        public readonly SourceString Label;
        public readonly LinkedList<Either<SourceChar[], Block>> Contents;

        public Block(string label, LinkedList<Block> blocks)
        {
            Label = label;
            Blocks = blocks;
        }
    }
}
