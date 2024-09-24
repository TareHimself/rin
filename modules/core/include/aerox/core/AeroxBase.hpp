#pragma once

#include <memory>
#include "Disposable.hpp"
#include "aerox/core/memory.hpp"
#include "meta/MetaClass.hpp"
class AeroxBase : public Disposable 
{
    
public:
    virtual Shared<MetaClass> GetMeta() const;
};
