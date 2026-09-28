using CCC.Domain.ValueObjects;

namespace CCC.Commands.SubscriptionModels;

public record AddSubscriptionModel(SubscriptionModelId Id, string Name, KeyLimit KeyLimit);
