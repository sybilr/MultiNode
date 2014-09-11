using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SybilResearch.MultiNode.SampleCustomerCode
{
    public class SentenceRepeater
    {
        public static void RepeatSentencesWithParams(Dictionary<string, object> args)
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine("Customer> This is a sample sentence that needs to be printed by customer");
                System.Threading.Thread.Sleep(300);
            }
        }
    }
}
