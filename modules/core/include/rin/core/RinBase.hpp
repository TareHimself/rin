#pragma once

#include <memory>
#include "Disposable.hpp"
#include "rin/core/memory.hpp"
#include "meta/MetaClass.hpp"
class RinBase : public Disposable 
{
    
public:
    virtual Shared<MetaClass> GetMeta() const;
};
