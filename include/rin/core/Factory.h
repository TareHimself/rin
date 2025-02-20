#pragma once
#include <unordered_map>

#include "math/Mat4.h"

namespace rin
{
    template <typename TItem, typename TInternalKey, typename TPublicKey>
    class Factory
    {
        std::unordered_map<TInternalKey, TItem> _items{};
    protected:
        virtual TItem Create(const TPublicKey& key) = 0;
        virtual TInternalKey ToInternalKey(const TPublicKey& key) = 0;
    public:
        virtual ~Factory() = default;
        virtual TItem Get(const TPublicKey& key);
        [[nodiscard]] std::unordered_map<TInternalKey, TItem> GetItems() const;
    };

    template <typename TItem, typename TInternalKey, typename TPublicKey>
    TItem Factory<TItem, TInternalKey, TPublicKey>::Get(const TPublicKey& key)
    {
        auto internalKey = ToInternalKey(key);

        if (_items.contains(internalKey)) return _items.at(internalKey);

        return _items.emplace(internalKey, Create(key)).first->second;
    }

    template <typename TItem, typename TInternalKey, typename TPublicKey>
    std::unordered_map<TInternalKey, TItem> Factory<TItem, TInternalKey, TPublicKey>::GetItems() const
    {
        return _items;
    }
}
