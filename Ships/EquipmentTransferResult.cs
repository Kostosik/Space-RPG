namespace GameProject.Ships;

public enum EquipmentTransferResult
{
    Success,

    ShipDefinitionMismatch,
    SlotNotFound,
    SlotOccupied,

    EquipmentNotFoundInCargoHold,
    EquipmentDefinitionNotFound,
    EquipmentDefinitionMismatch,
    IncompatibleSlot,
    InvalidEquipmentLevel,
    EquipmentAlreadyInstalled,
    EquipmentNotInstalled,
    EquipmentAlreadyInCargoHold,

    CargoHoldRejected
}