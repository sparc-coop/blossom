namespace Sparc.Blossom.Data.Pouch.Server
{
    public class DatumSummary
    {
        public string id { get; set; }
        public string key { get; set; }
        public RevisionSummary value { get; set; }

        public class RevisionSummary
        {
            public string rev { get; set; }
        }

        public static List<DatumSummary> ToDatumSummary(List<dynamic> data)
        {
            List<DatumSummary> vm = new();

            foreach (var item in data)
            {
                vm.Add(ToDatumSummary(item));
            }

            return vm;
        }

        public static DatumSummary ToDatumSummary(dynamic data)
        {
            return new DatumSummary
            {
                id = data._id,
                key = data._id,
                value = new RevisionSummary
                {
                    rev = data._rev
                }
            };
        }
    }
}
