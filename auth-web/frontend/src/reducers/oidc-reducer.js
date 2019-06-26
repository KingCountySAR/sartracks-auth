import { reducer as oidc } from 'redux-oidc'
//import * as actions from '../actions/oidc-actions'

export default function oidcReducer(state = {}, action) {
  const newState = oidc(state, action)

  switch (action.type) {
    default:
      return newState
  }
}