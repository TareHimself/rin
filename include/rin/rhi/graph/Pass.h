#pragma once
#include <cstdint>

#include "CompiledGraph.h"
#include "GraphBuilder.h"
#include "rin/rhi/Frame.h"

namespace rin::rhi
{
    class Pass
    {
    private:
        friend GraphBuilder;
        uint64_t _id{0};
    public:
        virtual ~Pass() = default;
        uint64_t GetId() const;

        virtual void Configure(GraphBuilder& builder) = 0;

        /**
         * Executes this pass in the render graph
         */
        virtual void Execute(Frame * frame, CompiledGraph* graph) = 0;
        /**
         * Returns true if this pass is a valid end of the render graph
         * @return true if this pass is terminal
         */
        virtual bool IsTerminal() = 0;
    };

}
