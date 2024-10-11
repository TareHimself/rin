#pragma once
#include <memory>
#include <optional>
#include <string>
#include <unordered_map>
#include <vector>
#include "USDFItem.hpp"
#include "rin/core/Disposable.hpp"
#include "rin/graphics/Image.hpp"

class USDFContainer : public Disposable
{
    std::unordered_map<std::string, USDFItem> _items{};
    std::vector<int> _atlases{};

public:
    USDFContainer();

    USDFContainer(const std::unordered_map<std::string, USDFItem>& items, const std::vector<int>& atlases);

    bool Has(const std::string& id) const;

    USDFItem Get(const std::string& id);

    void OnDispose(bool manual) override;
};
