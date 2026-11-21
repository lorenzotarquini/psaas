import type { Module, ModuleManifest } from '@/entities/catalog'

// UC15-R7: the manifest of a module as the catalog has it now, to edit it in place (the id stays).
export function toManifest(module: Module): ModuleManifest {
  return {
    id: module.id,
    name: module.name,
    description: module.description,
    version: module.version,
    icon: module.icon,
    status: module.status,
    requires: module.requires,
    permissions: module.permissions,
    provisionUrl: module.provisionUrl,
    deprovisionUrl: module.deprovisionUrl,
    tiers: module.tiers,
  }
}
