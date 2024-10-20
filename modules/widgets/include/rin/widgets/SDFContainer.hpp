#pragma once
#include <memory>
#include <string>
#include <unordered_map>
#include <vector>

#include "SDFItem.hpp"
#include "rin/core/Disposable.hpp"
#include "rin/graphics/Image.hpp"

class USDFContainer;

class SDFContainer
{
    std::unordered_map<std::string, SDFItem> _items{};
    std::vector<std::shared_ptr<Image<unsigned char>>> _atlases{};

public:
    SDFContainer();

    [[nodiscard]] bool Has(const std::string& id) const;

    SDFItem Get(const std::string& id);

    void AddAtlas(const std::shared_ptr<Image<unsigned char>>& atlas);
    void AddItem(const SDFItem& item);

    Shared<USDFContainer> Upload();
};
