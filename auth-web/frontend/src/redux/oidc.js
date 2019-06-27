import { reducer as oidc } from 'redux-oidc';

export const CHECKING = 'oidc/CHECKING';
//export const SIGNING_OUT = 'SIGNING_OUT'
export const RECEIVE_ROLES = 'RECEIVE_ROLES';

export function reducer(state = {}, action) {
  const newState = oidc(state, action)

  switch (action.type) {
    default:
      return newState
  }
}