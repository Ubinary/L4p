using System;
using System.Runtime.CompilerServices;

namespace L4p.Common.GcCaches
{
    public interface IGcCache<TFacet, TInstance>
        where TFacet : class
        where TInstance : class
    {
        void AddInstance(TFacet facet, TInstance instance);
        TInstance[] GetDeadInstances(TimeSpan ttl);
    }

    public class GcCache<TFacet, TInstance> : IGcCache<TFacet, TInstance>
        where TFacet : class
        where TInstance : class
    {
        #region members

        private readonly ConditionalWeakTable<TFacet, DeathNotifier> _deathAgent;
        private readonly IItemsRepo<TInstance> _repo;

        #endregion

        #region construction

        public static IGcCache<TFacet, TInstance> New()
        {
            return
                new GcCache<TFacet, TInstance>();
        }

        private GcCache()
        {
            _deathAgent = new ConditionalWeakTable<TFacet, DeathNotifier>();
            _repo = ItemsRepo<TInstance>.New();
        }

        #endregion

        #region private
        #endregion

        #region ITtlCache

        void IGcCache<TFacet, TInstance>.AddInstance(TFacet facet, TInstance instance)
        {
            var item = _repo.GetBy(instance);

            if (item == null)
            {
                item = TtlItem<TInstance>.New(instance);
                item = _repo.Add(item);
            }

            var reference = (IReferenceCounter) item;
            var notifier = new DeathNotifier(reference);

            // if death agent is already aware of the instance do nothing
            try
            {
                _deathAgent.Add(facet, notifier);
                reference.LinkInstance();
            }
            catch (ArgumentException)   // key already exists
            {
                notifier.Cancel();
            }
        }

        TInstance[] IGcCache<TFacet, TInstance>.GetDeadInstances(TimeSpan ttl)
        {
            var items = _repo.GetDeadItems(ttl);

            if (items == null)
                return null;

            if (items.Length == 0)
                return null;

            _repo.Remove(items);

            var count = items.Length;
            var instances = new TInstance[count];

            for (int indx = 0; indx < count; indx++)
            {
                instances[indx] = items[indx].Instance;
            }

            return instances;
        }

        #endregion
    }
}