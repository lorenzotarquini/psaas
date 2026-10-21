import { useQuery } from '@tanstack/react-query'
import { apiGet } from '@/shared/api'

// Mirrors ItalianCity in PSAAS.Application.
export interface City {
  name: string
  provinceCode: string
  postalCodes: string[]
}

export const minCitySearchLength = 2

// UC10: up to 20 cities; 400 below 2 characters (never sent), 503 when the city list is down.
export const useCities = (term: string) =>
  useQuery({
    queryKey: ['cities', term.toLowerCase()],
    queryFn: ({ signal }) =>
      apiGet<City[]>(`/api/geography/cities?term=${encodeURIComponent(term)}`, signal),
    enabled: term.length >= minCitySearchLength,
    staleTime: Infinity,
    retry: false,
  })
