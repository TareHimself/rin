float mapRangeClamped(float value, float fromMin, float fromMax, float toMin, float toMax) {
    // Ensure that the value is within the input range [fromMin, fromMax]
    value = clamp(value, fromMin, fromMax);

    // Calculate the normalized position of the value in the input range
    float normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return mix(toMin, toMax, normalizedPosition);
}

float mapRangeUnClamped(float value, float fromMin, float fromMax, float toMin, float toMax) {

    // Calculate the normalized position of the value in the input range
    float normalizedPosition = (value - fromMin) / (fromMax - fromMin);

    // Map the normalized position to the output range [toMin, toMax]
    return mix(toMin, toMax, normalizedPosition);
}