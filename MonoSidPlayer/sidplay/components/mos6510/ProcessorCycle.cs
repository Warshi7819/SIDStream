using System;

namespace sidplay
{
    public class ProcessorCycle
    {
        public delegate void FunctionDelegate();

        internal FunctionDelegate func;

        internal bool nosteal;

        internal ProcessorCycle()
        {
            func = null;
            nosteal = false;
        }
    }
}